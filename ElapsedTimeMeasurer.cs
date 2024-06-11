using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace ExcelTableConverter
{
    public static class ElapsedTimeMeasurer
    {
#if DEBUG
        public static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentBag<TimeSpan>>> Elapsed = new ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentBag<TimeSpan>>>();
#endif

        public static void Reset()
        {
#if DEBUG
            Elapsed.Clear();
#endif
        }

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

        public static string Display(string group)
        {
#if DEBUG
            var builder = new StringBuilder();

            if (Elapsed.TryGetValue(group, out var elapsedSet) == false)
                return string.Empty;

            var total = elapsedSet.ToDictionary(x => x.Key, x =>
            {
                var amount = TimeSpan.Zero;
                foreach (var t in x.Value)
                {
                    amount += t;
                }

                return TimeSpan.FromTicks(amount.Ticks);
            });

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

            var combined = TimeSpan.Zero;
            foreach (var t in total.Values)
            {
                combined += t;
            }

            foreach (var pair in total.Select(x => (Id: x.Key, Percentage: x.Value.TotalMilliseconds * 100 / combined.TotalMilliseconds, Total: total[x.Key])).OrderByDescending(x => x.Total))
            {
                builder.AppendLine($"{pair.Id} : {pair.Percentage:0.00}% ({average[pair.Id].TotalMilliseconds:0.00}ms / {total[pair.Id].TotalMilliseconds:0.00}ms / {elapsedSet[pair.Id].Count})");
            }

            return builder.ToString();
#else
            return string.Empty;
#endif
        }

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
