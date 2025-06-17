using ExcelTableConverter.Worker;
using System.Diagnostics;

namespace ExcelTableConverter
{
    /// <summary>
    /// Represents a scheduled task plan with execution function and error handling options
    /// </summary>
    public class Plan
    {
        /// <summary>
        /// Gets or sets the action to execute
        /// </summary>
        public Action Func { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether to stop execution on error
        /// </summary>
        public bool StopOnError { get; set; }
    }

    /// <summary>
    /// Static task scheduler for managing and executing queued operations
    /// 
    /// Provides centralized task scheduling with error handling, progress tracking,
    /// and execution timing capabilities for the Excel table conversion process.
    /// </summary>
    public static class Scheduler
    {
        private static readonly Queue<Plan> _actions = new Queue<Plan>();
        
        /// <summary>
        /// Gets the number of completed tasks
        /// </summary>
        public static int CompletedCount { get; private set; }
        
        private static readonly Stopwatch _timer = new Stopwatch();
        
        /// <summary>
        /// Gets the total number of scheduled jobs
        /// </summary>
        public static int Job { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the scheduler is suspended due to errors
        /// </summary>
        public static bool Suspended { get; private set; } = false;

        /// <summary>
        /// Adds a new task to the execution queue
        /// </summary>
        /// <param name="fn">The function to execute</param>
        /// <param name="stopOnError">Whether to stop execution if this task encounters an error</param>
        public static void Add(Action fn, bool stopOnError = false)
        {
            _actions.Enqueue(new Plan
            {
                StopOnError = stopOnError,
                Func = fn
            });
            Job++;
        }

        /// <summary>
        /// Executes all queued tasks in order
        /// 
        /// Processes each task in the queue, handling exceptions appropriately
        /// and tracking completion status. Execution may be suspended if critical
        /// errors occur or if a task is marked with StopOnError.
        /// </summary>
        public static void Run()
        {
            _timer.Start();
            while (_actions.TryDequeue(out var job))
            {
                try
                {
                    job.Func.Invoke();
                    CompletedCount++;
                }
                catch (Exception e)
                {
                    // Process exception hierarchy to handle nested exceptions
                    var queue = new Queue<Exception>();
                    queue.Enqueue(e);
                    while (queue.TryDequeue(out var error))
                    {
                        switch (error)
                        {
                            case AggregateException aggregateException:
                                foreach (var innerError in aggregateException.InnerExceptions)
                                {
                                    queue.Enqueue(innerError);
                                }
                                break;

                            case LogicException:
                                Logger.Error(error.Message);
                                break;

                            default:
                                Logger.Error(error.Message);
                                Logger.Error(error.StackTrace);
                                break;
                        }
                    }

                    Suspended = true;
                    if (job.StopOnError)
                        break;
                }
            }
        }

        /// <summary>
        /// Resets the scheduler state and clears all queued tasks
        /// </summary>
        public static void Reset()
        {
            _actions.Clear();
            Suspended = false;
        }

        /// <summary>
        /// Provides console decoration for progress display
        /// </summary>
        /// <param name="text">The text to decorate</param>
        /// <returns>Decorated text with timing and progress information</returns>
        public static string ConsoleDecorator(string text)
        {
            return $"[{_timer.Elapsed.ToString("mm\\:ss")} | {CompletedCount + 1,3}/{Job} | {ParallelWorker.Percent,3}%] {text}";
        }
    }
}
