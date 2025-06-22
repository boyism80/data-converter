using ExcelTableConverter.Model;

namespace ExcelTableConverter.Services
{
    /// <summary>
    /// Service interface for coordinating the Excel table conversion processing pipeline
    /// 
    /// Manages the sequential execution of data loading, validation, and code generation
    /// stages with proper error handling and progress tracking.
    /// </summary>
    public interface IProcessingPipelineService
    {
        /// <summary>
        /// Executes the complete data processing pipeline
        /// </summary>
        /// <param name="fileResult">The result of file processing containing categorized files</param>
        /// <param name="cachedContext">The cached context from previous runs</param>
        /// <param name="dslFilePath">The path to the DSL configuration file</param>
        /// <returns>The processed context ready for code generation</returns>
        /// <exception cref="InvalidOperationException">Thrown when pipeline execution fails</exception>
        Task<Context> ExecuteDataProcessingPipelineAsync(FileProcessingResult fileResult, Context cachedContext, string dslFilePath);

        /// <summary>
        /// Executes the validation pipeline
        /// </summary>
        /// <param name="context">The context to validate</param>
        /// <param name="fileResult">The result of file processing containing categorized files</param>
        /// <returns>True if validation passes, false otherwise</returns>
        bool ExecuteValidationPipeline(Context context, FileProcessingResult fileResult);

        /// <summary>
        /// Executes the code generation pipeline
        /// </summary>
        /// <param name="context">The validated context</param>
        /// <param name="targetLanguages">The target programming languages for code generation</param>
        /// <returns>True if code generation succeeds, false otherwise</returns>
        Task<bool> ExecuteCodeGenerationPipelineAsync(Context context, IReadOnlySet<string> targetLanguages);

        /// <summary>
        /// Updates data cache for the processed files
        /// </summary>
        /// <param name="context">The processed context</param>
        /// <param name="updatedFiles">The list of files that were updated</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task UpdateDataCacheAsync(Context context, IReadOnlyList<string> updatedFiles);

        /// <summary>
        /// Displays processing information and progress
        /// </summary>
        /// <param name="processFiles">The list of files being processed</param>
        /// <param name="updatedFiles">The list of updated files</param>
        /// <param name="errorFiles">The list of files with errors</param>
        void DisplayProcessingInfo(IReadOnlyList<string> processFiles, IReadOnlyList<string> updatedFiles, IReadOnlyList<string> errorFiles);
    }
}