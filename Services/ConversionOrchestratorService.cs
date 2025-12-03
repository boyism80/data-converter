using ExcelTableConverter.Configuration;
using ExcelTableConverter.Model;

namespace ExcelTableConverter.Services
{
    /// <summary>
    /// Orchestrates the complete Excel table conversion process
    /// 
    /// Coordinates the sequential execution of file processing, data loading,
    /// validation, and code generation stages with proper error handling.
    /// </summary>
    public class ConversionOrchestratorService
    {
        private readonly FileProcessingService _fileService;
        private readonly ProcessingPipelineService _pipelineService;
        private readonly AppConfiguration _config;

        /// <summary>
        /// Initializes a new instance of the ConversionOrchestratorService
        /// </summary>
        /// <param name="fileService">The file processing service</param>
        /// <param name="pipelineService">The processing pipeline service</param>
        /// <param name="config">The application configuration</param>
        public ConversionOrchestratorService(
            FileProcessingService fileService,
            ProcessingPipelineService pipelineService,
            AppConfiguration config)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _pipelineService = pipelineService ?? throw new ArgumentNullException(nameof(pipelineService));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Executes the complete Excel table conversion process
        /// </summary>
        /// <returns>True if the process succeeds, false otherwise</returns>
        public async Task<bool> ExecuteAsync()
        {
            try
            {
                // Load cached context
                var cachedContext = new Context(_config);
                var cacheLoaded = cachedContext.Load();
                var forceFullProcessing = cacheLoaded && cachedContext.BuildVersion != Context.BUILD_VERSION;

                // Process files and categorize them
                var fileResult = await _fileService.ProcessFilesAsync(
                    _config.InputDirectory,
                    cachedContext,
                    _config.DslFilePath,
                    forceFullProcessing);

                // Display processing information
                var errorFiles = fileResult.ErrorFiles;
                var updatedFiles = forceFullProcessing
                    ? fileResult.ProcessFiles.ToList()
                    : fileResult.ProcessFiles.Except(errorFiles).ToList();
                _pipelineService.DisplayProcessingInfo(fileResult, updatedFiles);

                // Execute data processing pipeline
                var processedContext = await _pipelineService.ExecuteDataProcessingPipelineAsync(
                    fileResult, cachedContext, _config.DslFilePath);

                // Save processed context to cache
                processedContext.Save();

                // Execute validation pipeline
                var validationSuccess = _pipelineService.ExecuteValidationPipeline(
                    processedContext, fileResult);

                if (!validationSuccess)
                {
                    await SaveErrorFilesAsync();
                    return false;
                }

                // Execute code generation pipeline
                var codeGenSuccess = await _pipelineService.ExecuteCodeGenerationPipelineAsync(
                    processedContext, _config.TargetLanguages);

                if (!codeGenSuccess)
                {
                    await SaveErrorFilesAsync();
                    return false;
                }

                // Update data cache if values were processed
                if (fileResult.ProcessFiles.Any())
                {
                    var cacheTargets = forceFullProcessing ? fileResult.ProcessFiles.ToList() : updatedFiles;
                    await _pipelineService.UpdateDataCacheAsync(processedContext, cacheTargets);
                }

                // Write performance metrics and clear error tracking
                await WritePerformanceMetricsAsync();
                await _fileService.SaveErrorFilesAsync(new List<string>());

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Conversion process failed: {ex.Message}");
                await SaveErrorFilesAsync();
                return false;
            }
        }

        /// <summary>
        /// Saves error files and performance metrics
        /// </summary>
        private async Task SaveErrorFilesAsync()
        {
            await _fileService.SaveErrorFilesAsync(Logger.ErrorFiles);
            await WritePerformanceMetricsAsync();
        }

        /// <summary>
        /// Writes performance metrics to file
        /// </summary>
        public static async Task WritePerformanceMetricsAsync()
        {
            try
            {
                var elapsedTimeContent = ElapsedTimeMeasurer.Display();
                await File.WriteAllTextAsync("ElapsedTime.txt", elapsedTimeContent);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to write performance metrics: {ex.Message}");
            }
        }
    }
}

