using ExcelTableConverter.Configuration;
using ExcelTableConverter.Model;

namespace ExcelTableConverter.Services
{
    /// <summary>
    /// Represents the result of file processing operations
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
        /// Gets or sets the loaded context with CRC information
        /// </summary>
        public Context LoadedContext { get; set; }

        /// <summary>
        /// Gets or sets whether the DSL file was changed
        /// </summary>
        public bool DslFileChanged { get; set; }
    }

    /// <summary>
    /// Service interface for processing Excel files and managing file operations
    /// 
    /// Handles file categorization, CRC checking for change detection,
    /// cache management, and file processing coordination.
    /// </summary>
    public interface IFileProcessingService
    {
        /// <summary>
        /// Processes Excel files in the specified directory and categorizes them
        /// </summary>
        /// <param name="inputDirectory">The directory containing Excel files</param>
        /// <param name="cachedContext">The cached context from previous runs</param>
        /// <param name="dslFilePath">The path to the DSL configuration file</param>
        /// <returns>A FileProcessingResult containing categorized files and processing information</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown when the input directory does not exist</exception>
        /// <exception cref="IOException">Thrown when file access fails</exception>
        Task<FileProcessingResult> ProcessFilesAsync(string inputDirectory, Context cachedContext, string dslFilePath);

        /// <summary>
        /// Loads or creates a cached context from the cache file
        /// </summary>
        /// <param name="configuration">The configuration service</param>
        /// <returns>The cached context or a new context if cache is invalid</returns>
        Task<Context> LoadCachedContextAsync(IConfigurationService configuration);

        /// <summary>
        /// Saves the context to the cache file
        /// </summary>
        /// <param name="context">The context to save</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task SaveContextToCacheAsync(Context context);

        /// <summary>
        /// Cleans up cache files for deleted, updated, and error files
        /// </summary>
        /// <param name="fileNames">The collection of file names to clean up</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task CleanupCacheFilesAsync(IEnumerable<string> fileNames);

        /// <summary>
        /// Loads the list of files that had errors in the previous run
        /// </summary>
        /// <returns>A list of file names that encountered errors</returns>
        Task<IReadOnlyList<string>> LoadErrorFilesAsync();

        /// <summary>
        /// Saves the list of files that encountered errors
        /// </summary>
        /// <param name="errorFiles">The collection of file names that had errors</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task SaveErrorFilesAsync(IEnumerable<string> errorFiles);
    }
}