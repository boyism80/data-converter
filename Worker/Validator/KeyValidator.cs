using ExcelTableConverter.Model;
using ExcelTableConverter.Util;
using System.Linq;

namespace ExcelTableConverter.Worker.Validator
{
    public class KeyValidator : ParallelWorker<RawSheetData, bool>
    {
        private readonly Mutex _mutex = new Mutex();
        private readonly Dictionary<string, List<(IExcelFileTrackable Tracker, object Key)>> _buffer = new Dictionary<string, List<(IExcelFileTrackable Tracker, object Key)>>();

        public KeyValidator(Context ctx) : base(ctx)
        {

        }

        protected override IEnumerable<RawSheetData> OnReady()
        {
            foreach (var rsd in Context.RawData.SelectMany(x => x.Value))
            {
                yield return rsd;
            }
        }

        protected override IEnumerable<bool> OnWork(RawSheetData sheet)
        {
            var (boldColumns, normalColumns) = sheet.Columns.Split();
            foreach (var columns in new[] { boldColumns, normalColumns })
            {
                if (columns == null)
                    continue;

                var pkList = columns.Where(x => Util.Type.IsPrimaryKey(x.Type, out _)).ToList();
                if (pkList.Count > 1)
                    throw new LogicException($"기본키가 2개 이상 정의되었습니다. ({string.Join(", ", pkList.ConvertAll(x => x.Name))})", sheet);

                foreach (var pk in pkList)
                {
                    if (Util.Type.IsNullable(pk.Type))
                        throw new LogicException($"기본키 {pk.Name}는 nullable 타입으로 정의할 수 없습니다.", sheet);
                }

                var gkList = columns.Where(x => Util.Type.IsGroupKey(x.Type, out _)).ToList();
                if (gkList.Count > 1)
                    throw new LogicException($"그룹키가 2개 이상 정의되었습니다. ({string.Join(", ", gkList.ConvertAll(x => x.Name))})", sheet);

                foreach (var gk in gkList)
                {
                    if (Util.Type.IsNullable(gk.Type))
                        throw new LogicException($"기본키 {gk.Name}는 nullable 타입으로 정의할 수 없습니다.", sheet);
                }
            }

            var boldKeyColumn = boldColumns?.FirstOrDefault(x => Util.Type.IsPrimaryKey(x.Type, out _));
            var normalKeyColumn = normalColumns?.FirstOrDefault(x => Util.Type.IsPrimaryKey(x.Type, out _));

            if (boldKeyColumn != null)
            {
                var values = boldKeyColumn.RowValuePairs.Values;
                if (Context.Result.Enum.ContainsKey(Util.Type.Nake(boldKeyColumn.Type)))
                {
                    var combinedEnumKeys = values.Where(x => Util.Enum.Combined(x as string)).Select(x => x as string).ToList();
                    if (combinedEnumKeys.Count > 0)
                        throw new AggregateException(combinedEnumKeys.Select(key => new LogicException($"키에 열거형 조합({key})를 사용할 수 없습니다.", sheet)));
                }

                var duplicatedList = values.GroupBy(x => x).Where(x => x.Skip(1).Any()).Select(x => x.ToList()).ToList();
                if (duplicatedList.Count > 0)
                    throw new AggregateException(duplicatedList.ConvertAll(duplicated => new LogicException($"키 '{duplicated[0]}'가 중복되었습니다.", sheet)));
            }

            if (normalKeyColumn != null)
            {
                if (boldColumns != null)
                {
                    var gk = boldColumns.FirstOrDefault(x => Util.Type.IsPrimaryKey(x.Type, out _));
                    var values = normalKeyColumn.RowValuePairs.Select(pair =>
                    {
                        var row = pair.Key;
                        var value = pair.Value;
                        var parent = gk.RowValuePairs.Where(ppair => ppair.Key < row).OrderByDescending(x => x.Key).First().Value;

                        return (parent, value);
                    }).ToList();

                    var duplicatedList = values.GroupBy(x => x).Where(x => x.Skip(1).Any()).Select(x => x.ToList()).ToList();
                    if (duplicatedList.Count > 0)
                        throw new AggregateException(duplicatedList.ConvertAll(duplicated => new LogicException($"키 '{duplicated[0]}'가 중복되었습니다.", sheet)));

                    _mutex.WaitOne();
                    if (_buffer.TryGetValue(sheet.TableName, out var keys) == false)
                    {
                        keys = new List<(IExcelFileTrackable, object)>();
                        _buffer.Add(sheet.TableName, keys);
                    }
                    keys.AddRange(values.Select(x => (sheet as IExcelFileTrackable, x as object)));
                    _mutex.ReleaseMutex();
                }
                else
                {
                    var values = normalKeyColumn.RowValuePairs.Values;
                    if (Context.Result.Enum.ContainsKey(Util.Type.Nake(normalKeyColumn.Type)))
                    {
                        var combinedEnumKeys = values.Where(x => Util.Enum.Combined(x as string)).Select(x => x as string).ToList();
                        if (combinedEnumKeys.Count > 0)
                            throw new AggregateException(combinedEnumKeys.Select(key => new LogicException($"키에 열거형 조합({key})를 사용할 수 없습니다.", sheet)));
                    }

                    var duplicatedList = values.GroupBy(x => x).Where(x => x.Skip(1).Any()).Select(x => x.ToList()).ToList();
                    if (duplicatedList.Count > 0)
                        throw new AggregateException(duplicatedList.ConvertAll(duplicated => new LogicException($"키 '{duplicated[0]}'가 중복되었습니다.", sheet)));

                    _mutex.WaitOne();
                    if (_buffer.TryGetValue(sheet.TableName, out var keys) == false)
                    {
                        keys = new List<(IExcelFileTrackable, object)>();
                        _buffer.Add(sheet.TableName, keys);
                    }
                    keys.AddRange(values.Select(x => (sheet as IExcelFileTrackable, x as object)));
                    _mutex.ReleaseMutex();
                }
            }

            yield return true;
        }

        protected override void OnWorked(RawSheetData input, bool output, int percent)
        {
            Logger.Write("키 중복 정의 여부를 검사중입니다.");
            base.OnWorked(input, output, percent);
        }

        protected override IReadOnlyList<bool> OnFinish(IReadOnlyList<bool> output)
        {
            var errors = new List<(IExcelFileTrackable Tracker, Exception Error)>();
            foreach (var (table, pair) in _buffer)
            {
                var duplicatedList = pair.GroupBy(x => x.Key).Where(x => x.Skip(1).Any()).ToDictionary(x => x.Key, x => x.Select(x => x.Tracker).ToList());
                foreach (var (key, trackers) in duplicatedList)
                {
                    var roots = string.Join(", ", trackers.Select(x => $"{x.FileName}:{x.SheetName}"));
                    errors.Add((trackers[0], new Exception($"키 {key}가 중복 정의되었습니다. ({roots})")));
                }
            }

            if (errors.Count > 0)
            {
                foreach (var (tracker, error) in errors)
                {
                    Logger.Error(error.Message, tracker);
                }
                throw new AggregateException(errors.Select(x => x.Error));
            }

            Logger.Complete("키 중복 정의 여부 검사를 완료했습니다.");
            return base.OnFinish(output);
        }

        protected override void OnError(RawSheetData input, Exception e, IExcelFileTrackable tracker = null)
        {
            base.OnError(input, e, input);
        }
    }
}
