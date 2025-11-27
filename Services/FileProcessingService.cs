using ExcelTableConverter.Configuration;
using ExcelTableConverter.Controller;
using ExcelTableConverter.Model;
using Force.Crc32;
using Newtonsoft.Json;

namespace ExcelTableConverter.Services
{
    /// <summary>
    /// Represents the result of file processing operations including deleted files management
    /// </summary>
    public class FileProcessingResult
    {
        /// <summary>
        /// Gets or sets the list of constant files to process
        /// </summary>
        public IReadOnlyList<string> ConstFiles { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of enum files to process
        /// </summary>
        public IReadOnlyList<string> EnumFiles { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of data files to process
        /// </summary>
        public IReadOnlyList<string> DataFiles { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of files that need to be processed (changed or error files)
        /// </summary>
        public IReadOnlyList<string> ProcessFiles { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of files that were deleted
        /// </summary>
        public IReadOnlyList<string> DeletedFiles { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the source controller containing data from deleted files.
        /// This includes Data, Enum, Const, and CRC information from files that were removed,
        /// preserved for dependency validation purposes.
        /// </summary>
        public SourceController DeletedFilesSource { get; set; }

        /// <summary>
        /// Gets or sets the loaded context with CRC information
        /// </summary>
        public Context LoadedContext { get; set; }

        /// <summary>
        /// Gets or sets whether the DSL file was changed
        /// </summary>
        public bool DslFileChanged { get; set; }

        /// <summary>
        /// Gets or sets the list of files that had errors in the previous run.
        /// </summary>
        public IReadOnlyList<string> ErrorFiles { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets whether this execution forces a full rebuild regardless of file changes.
        /// </summary>
        public bool IsFullRun { get; set; }
    }

    /// <summary>
    /// Implementation of file processing service for Excel table conversion
    /// 
    /// Handles file categorization, CRC-based change detection, cache management,
    /// and coordination of file processing operations.
    /// </summary>
    public class FileProcessingService
    {
        private readonly AppConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the FileProcessingService
        /// </summary>
        /// <param name="configuration">The application configuration</param>
        public FileProcessingService(AppConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }
        /// <summary>
        /// Processes Excel files in the specified directory and categorizes them.
        /// Handles deleted files by moving their data to a separate SourceController for dependency validation.
        /// </summary>
        /// <param name="inputDirectory">The directory containing Excel files</param>
        /// <param name="cachedContext">The cached context from previous runs</param>
        /// <param name="dslFilePath">The path to the DSL configuration file</param>
        /// <returns>A FileProcessingResult containing categorized files, processing information, and deleted files data</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown when the input directory does not exist</exception>
        /// <exception cref="IOException">Thrown when file access fails</exception>
        public async Task<FileProcessingResult> ProcessFilesAsync(
            string inputDirectory,
            Context cachedContext,
            string dslFilePath,
            bool forceFullProcessing = false)
        {
            if (!Directory.Exists(inputDirectory))
            {
                throw new DirectoryNotFoundException($"Input directory does not exist: {inputDirectory}");
            }

            var result = new FileProcessingResult();
            var loaded = new Context(cachedContext.Configuration);
            var dslFileKey = "dsl.json";

            var enumFiles = new List<string>();
            var constFiles = new List<string>();
            var dataFiles = new List<string>();
            var paths = Directory.GetFiles(inputDirectory, "*.xlsx", SearchOption.TopDirectoryOnly);

            foreach (var path in paths)
            {
                try
                {
                    // Skip temporary Excel files (starting with ~$)
                    var fileName = Path.GetFileName(path);
                    if (fileName.StartsWith("~$"))
                        continue;

                    var bytes = await File.ReadAllBytesAsync(path);
                    var crc = $"{Crc32Algorithm.Compute(bytes)}.{bytes.Length}";

                    if (!forceFullProcessing &&
                        cachedContext.Source.CRC.TryGetValue(fileName, out var oldCrc) &&
                        oldCrc == crc)
                    {
                        continue;
                    }

                    // Categorize files based on filename prefix
                    if (fileName.StartsWith(_configuration.ConstFilePrefix))
                    {
                        constFiles.Add(path);
                    }
                    else if (fileName.StartsWith(_configuration.EnumFilePrefix))
                    {
                        enumFiles.Add(path);
                    }
                    else
                    {
                        dataFiles.Add(path);
                    }

                    loaded.Source.CRC.Add(fileName, crc);
                }
                catch (IOException ex)
                {
                    throw new IOException($"Cannot open file {Path.GetFileName(path)}", ex);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error processing file {Path.GetFileName(path)}", ex);
                }
            }

            // Handle deleted files - move their data to a separate SourceController
            var existingFileNames = paths.Select(p => Path.GetFileName(p)).ToHashSet();
            var deletedFiles = cachedContext.Source.CRC.Keys.Except(existingFileNames).ToList();
            deletedFiles.Remove(dslFileKey);

            // Create a SourceController for deleted files
            var deletedFilesSource = new SourceController(cachedContext);

            foreach (var deletedFile in deletedFiles)
            {
                // Move data to deleted files source before removing from cache
                if (cachedContext.Source.Data.TryGetValue(deletedFile, out var data))
                {
                    deletedFilesSource.Data.Add(deletedFile, data);
                }
                if (cachedContext.Source.Enum.Container.TryGetValue(deletedFile, out var enums))
                {
                    deletedFilesSource.Enum.Container.Add(deletedFile, enums);
                }
                if (cachedContext.Source.Const.Container.TryGetValue(deletedFile, out var consts))
                {
                    deletedFilesSource.Const.Container.Add(deletedFile, consts);
                }
                if (cachedContext.Source.CRC.TryGetValue(deletedFile, out var crc))
                {
                    deletedFilesSource.CRC.Add(deletedFile, crc);
                }

                RemoveFromCache(cachedContext, deletedFile);
            }

            // Handle updated files - remove from cache to force reprocessing
            var updatedFiles = loaded.Source.CRC.Keys.ToList();
            foreach (var updatedFile in updatedFiles)
            {
                RemoveFromCache(cachedContext, updatedFile);
            }

            // Load error files from previous run
            var errorFiles = await LoadErrorFilesAsync();

            // Clean up cache files for deleted, updated, and error files
            await CleanupCacheFilesAsync(deletedFiles.Concat(updatedFiles).Concat(errorFiles));

            // Determine files that need processing
            var processFiles = forceFullProcessing
                ? updatedFiles
                : updatedFiles.Concat(errorFiles).ToList();
            processFiles = processFiles.Distinct().ToList();

            // Check if DSL file has changed
            var dslFileBytes = await File.ReadAllBytesAsync(dslFilePath);
            var dslCrc = $"{Crc32Algorithm.Compute(dslFileBytes)}.{dslFileBytes.Length}";
            var dslFileChanged = false;
            if (cachedContext.Source.CRC.TryGetValue(dslFileKey, out var oldDslCrc))
                dslFileChanged = (oldDslCrc != dslCrc);
            else
                dslFileChanged = true;

            loaded.Source.CRC.Add(dslFileKey, dslCrc);
            cachedContext.Source.CRC.Remove(dslFileKey);

            result.ConstFiles = constFiles;
            result.EnumFiles = enumFiles;
            result.DataFiles = dataFiles;
            result.ProcessFiles = processFiles;
            result.DeletedFiles = deletedFiles;
            result.DeletedFilesSource = deletedFilesSource;
            result.LoadedContext = loaded;
            result.ErrorFiles = forceFullProcessing ? new List<string>() : errorFiles;
            result.IsFullRun = forceFullProcessing;
            result.DslFileChanged = forceFullProcessing || dslFileChanged;

            return result;
        }

        /// <summary>
        /// Cleans up cache files for deleted, updated, and error files
        /// </summary>
        /// <param name="fileNames">The collection of file names to clean up</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task CleanupCacheFilesAsync(IEnumerable<string> fileNames)
        {
            var cleanupTasks = fileNames.Select(async fileName =>
            {
                try
                {
                    var cacheFilePath = Context.GetCacheFilePath(fileName);
                    if (File.Exists(cacheFilePath))
                    {
                        await Task.Run(() => File.Delete(cacheFilePath));
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to cleanup cache file for {fileName}: {ex.Message}");
                }
            });

            await Task.WhenAll(cleanupTasks);
        }

        /// <summary>
        /// Loads the list of files that had errors in the previous run
        /// </summary>
        /// <returns>A list of file names that encountered errors</returns>
        public async Task<IReadOnlyList<string>> LoadErrorFilesAsync()
        {
            try
            {
                if (!File.Exists(Context.ERROR_CACHE_PATH))
                {
                    return new List<string>();
                }

                var errorFilesJson = await File.ReadAllTextAsync(Context.ERROR_CACHE_PATH);
                var errorFiles = JsonConvert.DeserializeObject<List<string>>(errorFilesJson);
                return errorFiles ?? new List<string>();
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load error files: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Saves the list of files that encountered errors
        /// </summary>
        /// <param name="errorFiles">The collection of file names that had errors</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task SaveErrorFilesAsync(IEnumerable<string> errorFiles)
        {
            try
            {
                var errorFilesJson = JsonConvert.SerializeObject(errorFiles.ToList());
                await File.WriteAllTextAsync(Context.ERROR_CACHE_PATH, errorFilesJson);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to save error files: {ex.Message}");
            }
        }

        /// <summary>
        /// Removes a file from all cached collections
        /// </summary>
        /// <param name="context">The context to remove the file from</param>
        /// <param name="fileName">The name of the file to remove</param>
        private static void RemoveFromCache(Context context, string fileName)
        {
            context.Source.Const.Remove(fileName);
            context.Source.Enum.Remove(fileName);
            context.Source.Data.Remove(fileName);
            context.Source.CRC.Remove(fileName);
        }

        /// <summary>
        /// Clears the entire cache directory
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        private static async Task ClearCacheDirectoryAsync()
        {
            try
            {
                if (Directory.Exists(Context.CACHE_DIRECTORY))
                {
                    var files = Directory.GetFiles(Context.CACHE_DIRECTORY, "*", SearchOption.TopDirectoryOnly);
                    var deleteTasks = files.Select(file => Task.Run(() => File.Delete(file)));
                    await Task.WhenAll(deleteTasks);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to clear cache directory: {ex.Message}");
            }
        }
    }
}