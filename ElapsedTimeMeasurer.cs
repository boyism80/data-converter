using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace ExcelTableConverter
{
    /// <summary>
    /// Static performance measurement utility for tracking elapsed time across operations
    /// 
    /// Provides thread-safe performance measurement capabilities with grouping and
    /// statistical analysis features. Only active in DEBUG builds to minimize
    /// performance overhead in release builds.
    /// </summary>
    public static class ElapsedTimeMeasurer
    {
#if DEBUG
        /// <summary>
        /// Thread-safe storage for elapsed time measurements grouped by category and operation
        /// </summary>
        public static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentBag<TimeSpan>>> Elapsed = new ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentBag<TimeSpan>>>();
#endif

        /// <summary>
        /// Resets all elapsed time measurements
        /// </summary>
        public static void Reset()
        {
#if DEBUG
            Elapsed.Clear();
#endif
        }

        /// <summary>
        /// Measures the execution time of an action and stores the result
        /// </summary>
        /// <param name="group">The measurement group category</param>
        /// <param name="id">The specific operation identifier</param>
        /// <param name="fn">The action to measure</param>
        /// <param name="condition">Whether to record the measurement (default: true)</param>
        public static void Measure(string group, string id, Action fn, bool condition = true)
        {
#if DEBUG
            var container = Elapsed.GetOrAdd(group, _ => new ConcurrentDictionary<string, ConcurrentBag<TimeSpan>>());
            var elapsedSet = container.GetOrAdd(id, _ => new ConcurrentBag<TimeSpan>());

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            fn();
            stopwatch.Stop();
            if (condition)
                elapsedSet.Add(stopwatch.Elapsed);
#else
            fn();
#endif
        }

        /// <summary>
        /// Measures the execution time of a function and returns its result
        /// </summary>
        /// <typeparam name="T">The return type of the function</typeparam>
        /// <param name="group">The measurement group category</param>
        /// <param name="id">The specific operation identifier</param>
        /// <param name="fn">The function to measure</param>
        /// <param name="condition">Whether to record the measurement (default: true)</param>
        /// <returns>The result of the measured function</returns>
        public static T Measure<T>(string group, string id, Func<T> fn, bool condition = true)
        {
#if DEBUG
            var container = Elapsed.GetOrAdd(group, _ => new ConcurrentDictionary<string, ConcurrentBag<TimeSpan>>());
            var elapsedSet = container.GetOrAdd(id, _ => new ConcurrentBag<TimeSpan>());

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var result = fn();
            stopwatch.Stop();

            if (condition)
                elapsedSet.Add(stopwatch.Elapsed);

            return result;
#else
            return fn();
#endif
        }

        /// <summary>
        /// Measures the execution time of an asynchronous task
        /// </summary>
        /// <param name="group">The measurement group category</param>
        /// <param name="id">The specific operation identifier</param>
        /// <param name="fn">The async task to measure</param>
        /// <param name="condition">Whether to record the measurement (default: true)</param>
        /// <returns>A task representing the measured operation</returns>
        public static async Task Measure(string group, string id, Func<Task> fn, bool condition = true)
        {
#if DEBUG
            var container = Elapsed.GetOrAdd(group, _ => new ConcurrentDictionary<string, ConcurrentBag<TimeSpan>>());
            var elapsedSet = container.GetOrAdd(id, _ => new ConcurrentBag<TimeSpan>());

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            await fn();
            stopwatch.Stop();
            if (condition)
                elapsedSet.Add(stopwatch.Elapsed);
#else
            await fn();
#endif
        }

        /// <summary>
        /// Measures the execution time of an asynchronous function and returns its result
        /// </summary>
        /// <typeparam name="T">The return type of the async function</typeparam>
        /// <param name="group">The measurement group category</param>
        /// <param name="id">The specific operation identifier</param>
        /// <param name="fn">The async function to measure</param>
        /// <param name="condition">Whether to record the measurement (default: true)</param>
        /// <returns>A task representing the measured operation with its result</returns>
        public static async Task<T> Measure<T>(string group, string id, Func<Task<T>> fn, bool condition = true)
        {
#if DEBUG
            var container = Elapsed.GetOrAdd(group, _ => new ConcurrentDictionary<string, ConcurrentBag<TimeSpan>>());
            var elapsedSet = container.GetOrAdd(id, _ => new ConcurrentBag<TimeSpan>());

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var result = await fn();
            stopwatch.Stop();

            if (condition)
                elapsedSet.Add(stopwatch.Elapsed);

            return result;
#else
            return await fn();
#endif
        }

        /// <summary>
        /// Generates a performance report for a specific measurement group
        /// </summary>
        /// <param name="group">The measurement group to report on</param>
        /// <returns>A formatted string containing performance statistics</returns>
        public static string Display(string group)
        {
#if DEBUG
            var builder = new StringBuilder();

            if (Elapsed.TryGetValue(group, out var elapsedSet) == false)
                return string.Empty;

            // Calculate total elapsed time for each operation
            var total = elapsedSet.ToDictionary(x => x.Key, x =>
            {
                var amount = TimeSpan.Zero;
                foreach (var t in x.Value)
                {
                    amount += t;
                }

                return TimeSpan.FromTicks(amount.Ticks);
            });

            // Calculate average elapsed time for each operation
            var average = elapsedSet.ToDictionary(x => x.Key, x =>
            {
                var count = x.Value.Count;
                if (count == 0)
                    return TimeSpan.Zero;

                var amount = TimeSpan.Zero;
                foreach (var t in x.Value)
                {
                    amount += t;
                }

                return TimeSpan.FromTicks(amount.Ticks / count);
            });

            // Calculate combined total time for percentage calculations
            var combined = TimeSpan.Zero;
            foreach (var t in total.Values)
            {
                combined += t;
            }

            // Generate formatted report sorted by total time
            foreach (var pair in total.Select(x => (Id: x.Key, Percentage: x.Value.TotalMilliseconds * 100 / combined.TotalMilliseconds, Total: total[x.Key])).OrderByDescending(x => x.Total))
            {
                builder.AppendLine($"{pair.Id} : {pair.Percentage:0.00}% ({average[pair.Id].TotalMilliseconds:0.00}ms / {total[pair.Id].TotalMilliseconds:0.00}ms / {elapsedSet[pair.Id].Count})");
            }

            return builder.ToString();
#else
            return string.Empty;
#endif
        }

        /// <summary>
        /// Generates a comprehensive performance report for all measurement groups
        /// </summary>
        /// <returns>A formatted string containing performance statistics for all groups</returns>
        public static string Display()
        {
#if DEBUG
            var builder = new StringBuilder();
            foreach (var group in Elapsed.Keys)
            {
                builder.AppendLine($"[{group}]");
                builder.AppendLine(Display(group));
                builder.AppendLine(Environment.NewLine);
            }

            return builder.ToString();
#else
            return string.Empty;
#endif
        }
    }
}
