using ExcelTableConverter.Model;
using ExcelTableConverter.Util;

namespace ExcelTableConverter.Worker.Validator
{
    public class CronValidationData
    {
        public IExcelFileTrackable Tracker { get; set; }
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public string Type { get; set; }
        public object Value { get; set; }
    }

    public class CronValidator : ParallelWorker<CronValidationData, bool>
    {
        public CronValidator(Context ctx) : base(ctx)
        {
        }

        protected override IEnumerable<CronValidationData> OnReady()
        {
            var dataTables = Context.Completed.Data.GetAllTableNames();
            foreach (var (tableName, schemaSet) in Context.Completed.Schema)
            {
                if (dataTables.Contains(tableName) == false)
                    continue;

                foreach (var (columnName, schema) in schemaSet)
                {
                    if (Cron.IsCronType(schema.Type) == false)
                        continue;

                    var tracker = Context.Source.Data
                        .SelectMany(x => x.Value)
                        .FirstOrDefault(x => x.TableName == tableName) as IExcelFileTrackable;

                    var values = Context.Completed.Data.GetValues(tableName, columnName);
                    foreach (var value in values)
                    {
                        yield return new CronValidationData
                        {
                            Tracker = tracker,
                            TableName = tableName,
                            ColumnName = columnName,
                            Type = schema.Type,
                            Value = value
                        };
                    }
                }
            }

            foreach (var (tableName, consts) in Context.Completed.Const)
            {
                foreach (var (constName, constData) in consts)
                {
                    if (Cron.IsCronType(constData.Type) == false)
                        continue;

                    var tracker = Context.Source.Const
                        .SelectMany(x => x.Value)
                        .FirstOrDefault(x => x.TableName == tableName && x.Name == constName) as IExcelFileTrackable;

                    yield return new CronValidationData
                    {
                        Tracker = tracker,
                        TableName = tableName,
                        ColumnName = constName,
                        Type = constData.Type,
                        Value = constData.Value
                    };
                }
            }
        }

        protected override IEnumerable<bool> OnWork(CronValidationData value)
        {
            if (Util.Value.IsNull(value.Value))
            {
                if (Util.Type.IsNullable(value.Type) == false)
                    throw new NullValueException(value.Type);

                yield return true;
                yield break;
            }

            var expression = value.Value is string s ? s.Trim() : $"{value.Value}".Trim();
            if (Cron.IsValid(expression, out var error) == false)
                throw new LogicException($"{value.TableName}.{value.ColumnName} 값 '{expression}'은 올바른 cron 형식이 아닙니다. ({error})", value.Tracker);

            yield return true;
        }

        protected override void OnWorked(CronValidationData input, bool output, int percent)
        {
            Logger.Write($"cron 형식을 검사했습니다. - {input.TableName}.{input.ColumnName}");
        }

        protected override void OnError(CronValidationData input, Exception e, IExcelFileTrackable tracker = null)
        {
            base.OnError(input, e, input.Tracker ?? tracker);
        }

        protected override IReadOnlyList<bool> OnFinish(IReadOnlyList<bool> output)
        {
            Logger.Complete("cron 형식 검사를 완료했습니다.");
            return base.OnFinish(output);
        }
    }
}
