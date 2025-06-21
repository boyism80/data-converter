using ExcelTableConverter.Model;
using ExcelTableConverter.Util;
using Force.Crc32;
using Newtonsoft.Json;

namespace ExcelTableConverter.Services
{
    /// <summary>
    /// Implementation of file processing service for Excel table conversion
    /// 
    /// Handles file categorization, CRC-based change detection, cache management,
    /// and coordination of file processing operations.
    /// </summary>
    public class FileProcessingService : IFileProcessingService
    {
        /// <summary>
        /// Processes Excel files in the specified directory and categorizes them
        /// </summary>
        /// <param name="inputDirectory">The directory containing Excel files</param>
        /// <param name="cachedContext">The cached context from previous runs</param>
        /// <returns>A FileProcessingResult containing categorized files and processing information</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown when the input directory does not exist</exception>
        /// <exception cref="IOException">Thrown when file access fails</exception>
        public async Task<FileProcessingResult> ProcessFilesAsync(string inputDirectory, Context cachedContext)
        {
            if (!Directory.Exists(inputDirectory))
            {
                throw new DirectoryNotFoundException($"Input directory does not exist: {inputDirectory}");
            }

            var result = new FileProcessingResult();
            var loaded = new Context();

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

                    // Calculate CRC32 checksum for change detection
                    var bytes = await File.ReadAllBytesAsync(path);
                    var crc = $"{Crc32Algorithm.Compute(bytes)}.{bytes.Length}";

                    // Skip processing if file hasn't changed (same CRC)
                    if (cachedContext.CRC.TryGetValue(fileName, out var oldCrc) && oldCrc == crc)
                        continue;

                    // Categorize files based on filename prefix
                    if (fileName.StartsWith(Context.Config.ConstFilePrefix))
                    {
                        constFiles.Add(path);
                    }
                    else if (fileName.StartsWith(Context.Config.EnumFilePrefix))
                    {
                        enumFiles.Add(path);
                    }
                    else
                    {
                        dataFiles.Add(path);
                    }

                    loaded.CRC.Add(fileName, crc);
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

            // Handle deleted files - remove from cache
            var existingFileNames = paths.Select(p => Path.GetFileName(p)).ToHashSet();
            var deletedFiles = cachedContext.CRC.Keys.Except(existingFileNames).ToList();
            
            foreach (var deletedFile in deletedFiles)
            {
                RemoveFromCache(cachedContext, deletedFile);
            }

            // Handle updated files - remove from cache to force reprocessing
            var updatedFiles = loaded.CRC.Keys.ToList();
            foreach (var updatedFile in updatedFiles)
            {
                RemoveFromCache(cachedContext, updatedFile);
            }

            // Load error files from previous run
            var errorFiles = await LoadErrorFilesAsync();

            // Clean up cache files for deleted, updated, and error files
            await CleanupCacheFilesAsync(deletedFiles.Concat(updatedFiles).Concat(errorFiles));

            // Determine files that need processing
            var processFiles = updatedFiles.Concat(errorFiles).ToList();

            result.ConstFiles = constFiles;
            result.EnumFiles = enumFiles;
            result.DataFiles = dataFiles;
            result.ProcessFiles = processFiles;
            result.LoadedContext = loaded;

            return result;
        }

        /// <summary>
        /// Loads or creates a cached context from the cache file
        /// </summary>
        /// <returns>The cached context or a new context if cache is invalid</returns>
        public async Task<Context> LoadCachedContextAsync()
        {
            try
            {
                if (!File.Exists(Context.RAW_CACHE_PATH))
                {
                    return new Context();
                }

                var cacheBytes = await File.ReadAllBytesAsync(Context.RAW_CACHE_PATH);
                var cached = ZipUtil.Unzip<Context>(cacheBytes);

                // Check if build version has changed and clear cache if necessary
                if (cached.BuildVersion != Context.BUILD_VERSION)
                {
                    await ClearCacheDirectoryAsync();
                    Logger.WriteLine(" 컨버터 빌드 버전이 변경되어 캐시파일을 전부 제거했습니다.", 
                        foreground: ConsoleColor.Blue, decorate: false);
                    return new Context();
                }

                return cached;
            }
            catch (Exception)
            {
                // If cache loading fails, return new context
                return new Context();
            }
        }

        /// <summary>
        /// Saves the context to the cache file
        /// </summary>
        /// <param name="context">The context to save</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task SaveContextToCacheAsync(Context context)
        {
            try
            {
                if (File.Exists(Context.RAW_CACHE_PATH))
                {
                    File.Delete(Context.RAW_CACHE_PATH);
                }

                var cacheBytes = ZipUtil.Zip(context);
                await File.WriteAllBytesAsync(Context.RAW_CACHE_PATH, cacheBytes);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to save context to cache: {ex.Message}");
            }
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
            context.RawConst.Remove(fileName);
            context.RawEnum.Remove(fileName);
            context.RawData.Remove(fileName);
            context.CRC.Remove(fileName);
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