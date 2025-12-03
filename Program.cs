using ExcelTableConverter.Configuration;
using ExcelTableConverter.Model;
using ExcelTableConverter.Services;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;

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

                // Setup services
                using var serviceProvider = ConfigureServices(config);

                // Clear console if running in TTY mode
                if (Logger.TTY)
                {
                    Console.Clear();
                }

                // Execute the conversion process
                var orchestrator = serviceProvider.GetRequiredService<ConversionOrchestratorService>();
                var success = await orchestrator.ExecuteAsync();

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
        /// Configures the dependency injection container with services using ASP.NET Core DI
        /// </summary>
        /// <param name="appConfig">The application configuration</param>
        /// <returns>Service provider with all services registered</returns>
        private static ServiceProvider ConfigureServices(AppConfiguration appConfig)
        {
            var services = new ServiceCollection();

            // Register AppConfiguration as singleton
            services.AddSingleton(appConfig);

            // Register services as singletons
            services.AddSingleton<FileProcessingService>();
            services.AddSingleton<ProcessingPipelineService>();
            services.AddSingleton<ConversionOrchestratorService>();

            return services.BuildServiceProvider();
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

            await ConversionOrchestratorService.WritePerformanceMetricsAsync();
        }
    }
}
