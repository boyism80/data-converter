using ExcelTableConverter.Worker;
using System.Diagnostics;

namespace ExcelTableConverter
{
    public class Pipeline
    {
        private static readonly Stopwatch _elapsed = Stopwatch.StartNew();

        private readonly List<(Action Action, bool StopOnError)> _steps = new();
        private int _completed;

        public Pipeline Step(Action action, bool stopOnError = false)
        {
            _steps.Add((action, stopOnError));
            return this;
        }

        public bool Run()
        {
            Logger.OnDecorate = text => $"[{_elapsed.Elapsed:mm\\:ss} | {_completed + 1,3}/{_steps.Count} | {ParallelWorker.Percent,3}%] {text}";

            var succeeded = true;
            foreach (var (action, stopOnError) in _steps)
            {
                try
                {
                    action();
                    _completed++;
                }
                catch (Exception e)
                {
                    LogError(e);
                    succeeded = false;
                    if (stopOnError)
                        break;
                }
            }

            return succeeded;
        }

        private static void LogError(Exception e)
        {
            var queue = new Queue<Exception>();
            queue.Enqueue(e);
            while (queue.TryDequeue(out var error))
            {
                switch (error)
                {
                    case AggregateException aggregateException:
                        foreach (var inner in aggregateException.InnerExceptions)
                            queue.Enqueue(inner);
                        break;

                    case LogicException:
                    case NotImplementedException:
                        Logger.Error(error.Message);
                        break;

                    default:
                        Logger.Error(error.Message);
                        Logger.Error(error.StackTrace);
                        break;
                }
            }
        }
    }
}
