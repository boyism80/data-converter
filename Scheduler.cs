using ExcelTableConverter.Worker;
using System.Diagnostics;

namespace ExcelTableConverter
{
    public class Plan
    {
        public Action Func { get; set; }
        public bool StopOnError { get; set; }
    }

    public static class Scheduler
    {
        private static readonly Queue<Plan> _actions = new Queue<Plan>();
        public static int CompletedCount { get; private set; }
        private static readonly Stopwatch _timer = new Stopwatch();
        public static int Job { get; private set; }

        public static bool Suspended { get; private set; } = false;

        public static void Add(Action fn, bool stopOnError = false)
        {
            _actions.Enqueue(new Plan
            {
                StopOnError = stopOnError,
                Func = fn
            });
            Job++;
        }

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

        public static void Reset()
        {
            _actions.Clear();
            Suspended = false;
        }

        public static string ConsoleDecorator(string text)
        {
            return $"[{_timer.Elapsed.ToString("mm\\:ss")} | {CompletedCount + 1,3}/{Job} | {ParallelWorker.Percent,3}%] {text}";
        }
    }
}
