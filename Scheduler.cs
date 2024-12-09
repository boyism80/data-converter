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

        public static bool Suspended { get; private set; } = false;

        public static void Add(Action fn, bool stopOnError = false)
        {
            _actions.Enqueue(new Plan
            { 
                StopOnError = stopOnError,
                Func = fn
            });
            Logger.Job++;
        }

        public static void Run()
        {
            while (_actions.TryDequeue(out var job))
            {
                try
                {
                    job.Func.Invoke();
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
    }
}
