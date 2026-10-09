using ExcelTableConverter.Model;

namespace ExcelTableConverter.Worker.Validator
{
    public class StrongTypeValidationData
    {
        public IExcelFileTrackable Tracker { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public List<object> Values { get; set; }
    }

    public class StrongTypeValidator : ParallelWorker<StrongTypeValidationData, bool>
    {
        private readonly HashSet<string> _files = new HashSet<string>();

        public StrongTypeValidator(Context ctx, IEnumerable<string> files) : base(ctx)
        {
            _files = files.ToHashSet();
        }

        protected override IEnumerable<StrongTypeValidationData> OnReady()
        {
            foreach (var tableName in Context.Source.Data.TableNames)
            {
                foreach (var column in Context.Source.Data.FindColumns(tableName)) // TODO: cache
                {
                    var strong = column.ColumnType.Strong;
                    if (strong == null)
                        continue;

                    var tracker = Context.Source.Data.FindSheet(column);
                    if (_files.Contains(tracker.FileName) == false)
                        continue;

                    yield return new StrongTypeValidationData
                    {
                        Tracker = Context.Source.Data.FindSheet(column),
                        Type = strong,
                        Name = column.Name,
                        Values = column.RowValuePairs.Values.ToList()
                    };
                }
            }

            yield break;
        }

        protected override IEnumerable<bool> OnWork(StrongTypeValidationData value)
        {
            var columnType = ColumnType.Parse(value.Type);
            if (columnType.Relation != null)
            {
                var rel = columnType.Relation;
                var tableName = columnType.RelationTable ?? throw new LogicException($"{rel}은 올바른 테이블 형식이 아닙니다.", value.Tracker);
                var columnName = columnType.RelationColumn;

                if (Context.Completed.Schema.ContainsKey(tableName) == false)
                    throw new LogicException($"{rel}은 정의되지 않은 테이블입니다.", value.Tracker);

                var keyName = Context.Completed.Schema.FindKey(tableName)?.Name; // TODO: cache
                if (keyName == null)
                    throw new LogicException($"강연결 타입은 키가 정의된 테이블이어야 합니다. {tableName} 테이블은 키가 정의되지 않은 테이블입니다.", value.Tracker);

                if (columnName != null && keyName != columnName)
                    throw new LogicException($"강연결 타입은 반드시 테이블의 키와 연결되어야 합니다.", value.Tracker);

                var keys = Context.Completed.Data.Column(tableName, keyName).Select(x => $"{x}"); // TODO: caching
                var values = value.Values.ConvertAll(x => $"{x}");
                var diff = keys.Except(values).ToList();
                if (diff.Count > 0)
                    throw new LogicException($"강연결 타입 {value.Name}에 누락된 데이터가 있습니다. ({string.Join(", ", diff)})", value.Tracker);

                yield return true;
            }
            else if (Context.Completed.Enum.TryGetValue(value.Type, out var enums))
            {
                var diff = enums.Keys.Except(value.Values.Select(x => $"{x}")).ToList();
                if (diff.Count > 0)
                    throw new LogicException($"강연결 타입 {value.Name}에 누락된 데이터가 있습니다. ({string.Join(", ", diff)})", value.Tracker);

                yield return true;
            }
            else
            {
                throw new LogicException($"{value.Type}은 정의되지 않은 형식입니다.", value.Tracker);
            }

            yield return true;
        }

        protected override void OnWorked(StrongTypeValidationData input, bool output, int percent)
        {
            Logger.Write($"강연결 타입 데이터를 검사했습니다. - {input.Type}");
        }

        protected override void OnError(StrongTypeValidationData input, Exception e, IExcelFileTrackable tracker = null)
        {
            base.OnError(input, e, tracker);
        }

        protected override IReadOnlyList<bool> OnFinish(IReadOnlyList<bool> output)
        {
            Logger.Complete($"강연결 타입 데이터를 검사했습니다.");
            return base.OnFinish(output);
        }
    }
}
