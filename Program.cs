using System.ComponentModel.DataAnnotations;
using ExcelTableConverter.Configuration;
using ExcelTableConverter.Services;
using ExcelTableConverter.Util;

/// <summary>
/// Excel Table Converter - Main program entry point
/// 
/// This application converts Excel files to various programming language formats
/// including C++, C#, Node.js, and Go. It processes Excel files containing
/// game data tables and generates strongly-typed code representations.
/// </summary>
namespace ExcelTableConverter
{
    /// <summary>
    /// Main program class for the Excel Table Converter application
    /// </summary>
    public class Program
    {
        private static ServiceContainer _serviceContainer = new();

        /// <summary>
        /// Main entry point for the application
        /// </summary>
        /// <param name="args">Command-line arguments</param>
        /// <returns>Exit code (0 for success, 1 for failure)</returns>
        public static async Task<int> Main(string[] args)
        {
            // Initialize logger decorator
            Logger.OnDecorate = Scheduler.ConsoleDecorator;

            try
            {
                // Parse and validate configuration
                var config = AppConfiguration.Parse(args);
                config.ApplyToEnvironment();

                // Setup services
                ConfigureServices(config);

                // Clear console if running in TTY mode
                if (Logger.TTY)
                {
                    Console.Clear();
                }

                // Execute the conversion process
                var success = await ExecuteConversionProcessAsync(config);

                return success ? 0 : 1;
            }
            catch (ValidationException ex)
            {
                Logger.Error($"Configuration validation failed: {ex.Message}");
                return 1;
            }
            catch (ArgumentException ex)
            {
                Logger.Error($"Invalid arguments: {ex.Message}");
                return 1;
            }
            catch (Exception ex)
            {
                await HandleUnexpectedErrorAsync(ex);
                return 1;
            }
        }

        /// <summary>
        /// Configures the dependency injection container with services
        /// </summary>
        private static void ConfigureServices(AppConfiguration appConfig)
        {
            // Register configuration service as singleton
            var configService = new ConfigurationService(appConfig);
            _serviceContainer.RegisterInstance<IConfigurationService>(configService);

            // Register services with factory methods to handle constructor dependencies
            _serviceContainer.RegisterFactory<IFileProcessingService>(() =>
                new FileProcessingService(_serviceContainer.Resolve<IConfigurationService>()));

            _serviceContainer.RegisterFactory<IProcessingPipelineService>(() =>
                new ProcessingPipelineService(_serviceContainer.Resolve<IConfigurationService>()));
        }

        /// <summary>
        /// Executes the complete Excel table conversion process
        /// </summary>
        /// <param name="config">The application configuration</param>
        /// <returns>True if the process succeeds, false otherwise</returns>
        private static async Task<bool> ExecuteConversionProcessAsync(AppConfiguration config)
        {
            var fileService = _serviceContainer.Resolve<IFileProcessingService>();
            var pipelineService = _serviceContainer.Resolve<IProcessingPipelineService>();
            var configService = _serviceContainer.Resolve<IConfigurationService>();

            try
            {
                // Load cached context
                var cachedContext = await fileService.LoadCachedContextAsync(configService);

                // Process files and categorize them
                var fileResult = await fileService.ProcessFilesAsync(config.InputDirectory, cachedContext, config.DslFilePath);

                // Display processing information
                var errorFiles = await fileService.LoadErrorFilesAsync();
                var updatedFiles = fileResult.ProcessFiles.Except(errorFiles).ToList();
                pipelineService.DisplayProcessingInfo(fileResult.ProcessFiles, updatedFiles, errorFiles);

                // Execute data processing pipeline
                var processedContext = await pipelineService.ExecuteDataProcessingPipelineAsync(
                    fileResult, cachedContext, config.DslFilePath);

                // Save processed context to cache
                await fileService.SaveContextToCacheAsync(processedContext);

                // Execute validation pipeline
                var validationSuccess = await pipelineService.ExecuteValidationPipelineAsync(
                    processedContext, fileResult.ProcessFiles, fileResult.DslFileChanged);

                if (!validationSuccess)
                {
                    await SaveErrorFilesAsync(fileService);
                    return false;
                }

                // Execute code generation pipeline
                var codeGenSuccess = await pipelineService.ExecuteCodeGenerationPipelineAsync(
                    processedContext, config.TargetLanguages);

                if (!codeGenSuccess)
                {
                    await SaveErrorFilesAsync(fileService);
                    return false;
                }

                // Update data cache if values were processed
                if (fileResult.ProcessFiles.Any())
                {
                    await pipelineService.UpdateDataCacheAsync(processedContext, updatedFiles);
                }

                // Write performance metrics and clear error tracking
                await WritePerformanceMetricsAsync();
                await fileService.SaveErrorFilesAsync(new List<string>());

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Conversion process failed: {ex.Message}");
                await SaveErrorFilesAsync(fileService);
                return false;
            }
        }

        /// <summary>
        /// Saves error files and performance metrics
        /// </summary>
        /// <param name="fileService">The file processing service</param>
        private static async Task SaveErrorFilesAsync(IFileProcessingService fileService)
        {
            await fileService.SaveErrorFilesAsync(Logger.ErrorFiles);
            await WritePerformanceMetricsAsync();
        }

        /// <summary>
        /// Writes performance metrics to file
        /// </summary>
        private static async Task WritePerformanceMetricsAsync()
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

        /// <summary>
        /// Handles unexpected errors with detailed logging
        /// </summary>
        /// <param name="exception">The exception to handle</param>
        private static async Task HandleUnexpectedErrorAsync(Exception exception)
        {
            var queue = new Stack<Exception>();
            queue.Push(exception);

            while (queue.TryPop(out var error))
            {
                if (error is AggregateException aggregateException)
                {
                    foreach (var innerException in aggregateException.InnerExceptions)
                    {
                        queue.Push(innerException);
                    }
                    continue;
                }

                Logger.Error($"Exception Type: {error.GetType().Name}");
                Logger.Error($"Message: {error.Message}");

                if (!string.IsNullOrEmpty(error.StackTrace))
                {
                    Logger.Error($"Stack Trace: {error.StackTrace}");
                }

                if (error.InnerException != null)
                {
                    queue.Push(error.InnerException);
                }
            }

            await WritePerformanceMetricsAsync();
        }
    }
}
