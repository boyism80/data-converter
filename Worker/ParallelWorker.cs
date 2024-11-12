using ExcelTableConverter.Model;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Collections.Concurrent;

namespace ExcelTableConverter.Worker
{
    public abstract class ParallelWorker<T, R>
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        public Context Context { get; private set; }

        protected ParallelWorker(Context ctx)
        {
            Context = ctx;
        }

        protected abstract IEnumerable<T> OnReady();

        protected abstract IEnumerable<R> OnWork(T value);

        protected virtual void OnWorked(T input, R output, int percent)
        { }

        protected virtual int TotalCount(IReadOnlyList<T> inputs)
        {
            return inputs.Count;
        }

        protected virtual int RuntimeAdditionalCount()
        {
            return 0;
        }

        protected virtual void OnError(T input, Exception e, IExcelFileTrackable tracker = null)
        {
            switch (e)
            {
                case AggregateException ae:
                    foreach (var x in ae.InnerExceptions)
                        Logger.Error(x.Message, tracker);
                    break;

                default:
                    Logger.Error(e.Message, tracker);
                    break;
            }
        }

        protected virtual void OnStart(T input, int percent)
        { }

        protected virtual IReadOnlyList<R> OnFinish(IReadOnlyList<R> output)
        {
            return output;
        }

        private void Enqueue(T value)
        {
            _queue.Enqueue(value);
        }

        private int GetProgressPercent(IEnumerable<List<R>> output, int totalCount)
        {
            var goal = totalCount + RuntimeAdditionalCount();
            var count = output.Select(x => x.Count).DefaultIfEmpty(0).Sum();
            var percent = (int)Math.Max(0, Math.Min(100, goal > 0 ? (count * 100) / (double)goal : 0));
            return percent;
        }

        public IReadOnlyList<R> Run()
        {
            var mutexWorked = new Mutex();
            var mutexErrors = new Mutex();
            var buffer = new ConcurrentDictionary<int, List<R>>();
            var indices = new Dictionary<T, int>();
            foreach (var (input, i) in OnReady().Select((x, i) => (x, i)))
            {
                Enqueue(input);
                if (indices.ContainsKey(input) == false)
                    indices.Add(input, indices.Count);
            }

            var totalCount = TotalCount(_queue.ToList());
            var logicalErrors = new ConcurrentBag<LogicException>();
            var unhandledErrors = new ConcurrentBag<Exception>();
            var tasks = Enumerable.Range(0, Environment.ProcessorCount).Select(_ => new Task(() =>
            {
                while (true)
                {
                    var exists = _queue.TryDequeue(out var input);
                    if (exists == false)
                        break;

                    OnStart(input, GetProgressPercent(buffer.Values, totalCount));
                    var outputs = new List<R>();
                    var enumerator = OnWork(input).GetEnumerator();
                    while (true)
                    {
                        try
                        {
                            if (enumerator.MoveNext() == false)
                                break;

                            var output = enumerator.Current;
                            outputs.Add(output);

                            mutexWorked.WaitOne();
                            OnWorked(input, output, GetProgressPercent(buffer.Values, totalCount));
                            mutexWorked.ReleaseMutex();
                        }
                        catch (LogicException e)
                        {
                            mutexErrors.WaitOne();
                            try { OnError(input, e, e.Tracker); }
                            catch { }
                            logicalErrors.Add(e);
                            mutexErrors.ReleaseMutex();
                        }
                        catch (AggregateException e)
                        {
                            var stack = new Stack<Exception>();
                            stack.Push(e);

                            while (stack.TryPop(out var error))
                            {
                                switch (error)
                                {
                                    case AggregateException aggregateException:
                                        foreach (var inner in aggregateException.InnerExceptions)
                                        {
                                            stack.Push(inner);
                                        }
                                        break;

                                    case LogicException logicException:
                                        mutexErrors.WaitOne();
                                        try { OnError(input, logicException, logicException.Tracker); }
                                        catch { }
                                        logicalErrors.Add(logicException);
                                        mutexErrors.ReleaseMutex();
                                        break;

                                    default:
                                        unhandledErrors.Add(e);
                                        break;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            unhandledErrors.Add(e);
                        }
                    }

                    var index = indices[input];
                    buffer.AddOrUpdate(index, outputs, (key, old) =>
                    {
                        old.AddRange(outputs);
                        return old;
                    });
                }
            })).ToArray();

            foreach (var task in tasks)
            {
                task.Start();
            }

            Task.WaitAll(tasks);
            if (!unhandledErrors.IsEmpty)
            {
                throw new AggregateException(unhandledErrors);
            }

            var result = buffer.OrderBy(x => x.Key).SelectMany(x => x.Value).ToList();
            result = OnFinish(result) as List<R>;

            if (logicalErrors.IsEmpty)
            {
                return result;
            }
            else if (logicalErrors.Count == 1)
            {
                throw logicalErrors.First();
            }
            else
            {
                throw new AggregateException(logicalErrors);
            }
        }
    }

    public abstract class ParallelSheetLoader<R> : ParallelWorker<Sheet, R>
    {
        private readonly IReadOnlyList<Sheet> _sheets;

        protected ParallelSheetLoader(Context ctx, IReadOnlyList<Sheet> sheets) : base(ctx)
        {
            _sheets = sheets;
        }

        private static bool IsComment(ICell cell)
        {
            if (cell == null)
                return false;

            var cellType = cell.CellType;
            if (cellType == CellType.Formula)
                cellType = cell.CachedFormulaResultType;

            if (cellType == CellType.String)
                return cell.StringCellValue.StartsWith("#");

            if (cellType == CellType.Error)
                return (cell as XSSFCell).ErrorCellString.StartsWith("#");

            return false;
        }

        private static object GetValue(ICell cell)
        {
            var cellType = cell.CellType;
            if (cellType == CellType.Formula)
                cellType = cell.CachedFormulaResultType;

            switch (cellType)
            {
                case CellType.Numeric:
                    return $"{cell.NumericCellValue}" as object;

                case CellType.Boolean:
                    return cell.BooleanCellValue as object;

                case CellType.Blank:
                    return null;

                case CellType.String:
                    var s = cell.StringCellValue.Trim();
                    return string.IsNullOrEmpty(s) ? null : s;

                default:
                    throw new LogicException("invalid data type");
            }
        }

        protected object GetValue(ICell cell, string type)
        {
            switch (Util.Type.Nake(type))
            {
                case "int":
                case "long":
                    {
                        switch (cell.CellType)
                        {
                            case CellType.Numeric:
                            case CellType.Formula:
                                {
                                    var casted = (long)Math.Round(cell.NumericCellValue);
                                    var diff = Math.Abs(casted - cell.NumericCellValue);
                                    if (diff < 0.0001f)
                                        return casted;
                                    else
                                        return $"{Math.Round(cell.NumericCellValue, 4)}";
                                }

                            default:
                                return GetValue(cell);
                        }
                    }

                case "TimeSpan":
                    {
                        switch (cell.CellType)
                        {
                            case CellType.Numeric:
                            case CellType.Formula:
                                return cell.DateCellValue.TimeOfDay;

                            default:
                                return GetValue(cell);
                        }
                    }
                case "DateTime":
                    {
                        switch (cell.CellType)
                        {
                            case CellType.Numeric:
                            case CellType.Formula:
                                return cell.DateCellValue;

                            default:
                                return GetValue(cell);
                        }
                    }

                default:
                    return GetValue(cell);
            }
        }

        protected IReadOnlyDictionary<int, ICell> ReadLine(XSSFRow row)
        {
            var cellFirst = row.GetCell(0, MissingCellPolicy.RETURN_BLANK_AS_NULL);
            if (IsComment(cellFirst))
                return new Dictionary<int, ICell>();

            var result = new Dictionary<int, ICell>();
            for (int col = 0; col < row.LastCellNum; col++)
            {
                var cell = row.GetCell(col, MissingCellPolicy.RETURN_BLANK_AS_NULL);
                if (cell == null || cell.CellType == CellType.Blank)
                    continue;

                if (IsComment(cell))
                    continue;

                result.Add(col, cell);
            }

            return result;
        }

        protected override IEnumerable<Sheet> OnReady()
        {
            foreach (var sheet in _sheets)
            {
                yield return sheet;
            }
        }
    }
}
