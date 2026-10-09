using ExcelTableConverter.Model;

namespace ExcelTableConverter.Worker.Validator
{
    public class KeyValidator : ParallelWorker<SourceSheetData, bool>
    {
        private readonly Dictionary<string, List<(IExcelFileTrackable Tracker, object Key)>> _buffer = new Dictionary<string, List<(IExcelFileTrackable Tracker, object Key)>>();

        public KeyValidator(Context ctx) : base(ctx)
        { }

        protected override IEnumerable<SourceSheetData> OnReady()
        {
            foreach (var rsd in Context.Source.Data.SelectMany(x => x.Value))
            {
                yield return rsd;
            }
        }

        protected override IEnumerable<bool> OnWork(SourceSheetData sheet)
        {
            var (boldColumns, normalColumns) = sheet.Columns.Split();
            foreach (var columns in new[] { boldColumns, normalColumns })
            {
                if (columns == null)
                    continue;

                var pkList = columns.Where(x => x.ColumnType.PrimaryKey).ToList();
                if (pkList.Count > 1)
                    throw new LogicException($"기본키가 2개 이상 정의되었습니다. ({string.Join(", ", pkList.ConvertAll(x => x.Name))})", sheet);

                foreach (var pk in pkList)
                {
                    if (pk.ColumnType.Nullable)
                        throw new LogicException($"기본키 {pk.Name}는 nullable 타입으로 정의할 수 없습니다.", sheet);
                }

                var gkList = columns.Where(x => x.ColumnType.GroupKey).ToList();
                if (gkList.Count > 1)
                    throw new LogicException($"그룹키가 2개 이상 정의되었습니다. ({string.Join(", ", gkList.ConvertAll(x => x.Name))})", sheet);

                foreach (var gk in gkList)
                {
                    if (gk.ColumnType.Nullable)
                        throw new LogicException($"기본키 {gk.Name}는 nullable 타입으로 정의할 수 없습니다.", sheet);
                }
            }

            var boldKeyColumn = boldColumns?.FirstOrDefault(x => x.ColumnType.PrimaryKey);
            var normalKeyColumn = normalColumns?.FirstOrDefault(x => x.ColumnType.PrimaryKey);

            if (boldKeyColumn != null)
            {
                var values = boldKeyColumn.RowValuePairs.Values;
                if (Context.Completed.Enum.ContainsKey(boldKeyColumn.ColumnType.Naked))
                {
                    var combinedEnumKeys = values.Where(x => EnumExpression.IsCombined(x as string)).Select(x => x as string).ToList();
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
                    var gk = boldColumns.FirstOrDefault(x => x.ColumnType.PrimaryKey);
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

                    lock (_buffer)
                    {
                        if (_buffer.TryGetValue(sheet.TableName, out var keys) == false)
                        {
                            keys = new List<(IExcelFileTrackable, object)>();
                            _buffer.Add(sheet.TableName, keys);
                        }
                        keys.AddRange(values.Select(x => (sheet as IExcelFileTrackable, x as object)));
                    }
                }
                else
                {
                    var values = normalKeyColumn.RowValuePairs.Values;
                    if (Context.Completed.Enum.ContainsKey(normalKeyColumn.ColumnType.Naked))
                    {
                        var combinedEnumKeys = values.Where(x => EnumExpression.IsCombined(x as string)).Select(x => x as string).ToList();
                        if (combinedEnumKeys.Count > 0)
                            throw new AggregateException(combinedEnumKeys.Select(key => new LogicException($"키에 열거형 조합({key})를 사용할 수 없습니다.", sheet)));
                    }

                    var duplicatedList = values.GroupBy(x => x).Where(x => x.Skip(1).Any()).Select(x => x.ToList()).ToList();
                    if (duplicatedList.Count > 0)
                        throw new AggregateException(duplicatedList.ConvertAll(duplicated => new LogicException($"키 '{duplicated[0]}'가 중복되었습니다.", sheet)));

                    lock (_buffer)
                    {
                        if (_buffer.TryGetValue(sheet.TableName, out var keys) == false)
                        {
                            keys = new List<(IExcelFileTrackable, object)>();
                            _buffer.Add(sheet.TableName, keys);
                        }
                        keys.AddRange(values.Select(x => (sheet as IExcelFileTrackable, x as object)));
                    }
                }
            }

            yield return true;
        }

        protected override void OnWorked(SourceSheetData input, bool output, int percent)
        {
            Logger.Write("키 중복 정의 여부를 검사중입니다.");
            base.OnWorked(input, output, percent);
        }

        protected override IReadOnlyList<bool> OnFinish(IReadOnlyList<bool> output)
        {
            var errors = new List<(IExcelFileTrackable Tracker, Exception Error)>();
            foreach (var (table, pair) in _buffer)
            {
                foreach (var group in pair.GroupBy(x => x.Key))
                {
                    if (group.Count() <= 1)
                        continue;

                    var trackers = group.Select(x => x.Tracker).ToList();
                    var roots = string.Join(", ", trackers.Select(x => $"{x.FileName}:{x.SheetName}"));
                    errors.Add((trackers[0], new Exception($"키 {group.Key}가 중복 정의되었습니다. ({roots})")));
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

        protected override void OnError(SourceSheetData input, Exception e, IExcelFileTrackable tracker = null)
        {
            base.OnError(input, e, input);
        }
    }
}
