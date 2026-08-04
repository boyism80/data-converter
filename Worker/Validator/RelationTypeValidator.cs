using ExcelTableConverter.Model;

namespace ExcelTableConverter.Worker.Validator
{
    public class RelationTypeValidationData
    {
        public IExcelFileTrackable Tracker { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public uint Scope { get; set; }
    }

    public class RelationTypeValidator : ParallelWorker<RelationTypeValidationData, bool>
    {
        public RelationTypeValidator(Context ctx) : base(ctx)
        {
        }

        protected override IEnumerable<RelationTypeValidationData> OnReady()
        {
            var queue = new Queue<RelationTypeValidationData>();
            foreach (var sourceConst in Context.Source.Const.SelectMany(x => x.Value))
            {
                queue.Enqueue(new RelationTypeValidationData
                {
                    Tracker = sourceConst,
                    Name = sourceConst.Name,
                    Type = sourceConst.Type,
                    Scope = sourceConst.Scope
                });
            }

            foreach (var sourceData in Context.Source.Data.SelectMany(x => x.Value))
            {
                foreach (var column in sourceData.Columns)
                {
                    queue.Enqueue(new RelationTypeValidationData
                    {
                        Tracker = sourceData,
                        Name = column.Name,
                        Type = column.Type,
                        Scope = column.Scope
                    });
                }
            }

            while (queue.TryDequeue(out var rvd))
            {
                if (Util.Type.IsRelation(rvd.Type, out var rel))
                {
                    rvd.Type = rel;
                    yield return rvd;
                }
                else if (Util.Type.IsArray(rvd.Type, out var e))
                {
                    queue.Enqueue(new RelationTypeValidationData
                    {
                        Tracker = rvd.Tracker,
                        Name = rvd.Name,
                        Type = e,
                        Scope = rvd.Scope,
                    });
                }
                else if (Util.Type.IsMap(rvd.Type, out var pair))
                {
                    queue.Enqueue(new RelationTypeValidationData
                    {
                        Tracker = rvd.Tracker,
                        Name = rvd.Name,
                        Type = pair.Key,
                        Scope = rvd.Scope,
                    });

                    queue.Enqueue(new RelationTypeValidationData
                    {
                        Tracker = rvd.Tracker,
                        Name = rvd.Name,
                        Type = pair.Value,
                        Scope = rvd.Scope,
                    });
                }
                else
                { }
            }
        }

        protected override IEnumerable<bool> OnWork(RelationTypeValidationData value)
        {
            var refer = Util.Type.Nake(value.Type);
            if (Util.Type.SplitReferenceType(refer, out var tableName, out var columnName) == false)
                throw new LogicException("알 수 없는 에러", value.Tracker);

            if (Context.Completed.Schema.GetAllTableNames().Contains(tableName) == false)
                throw new LogicException($"{tableName}는 정의되지 않은 테이블입니다.", value.Tracker);

            if (Context.Completed.Schema.GetKeyTableNames().Contains(tableName) == false)
                throw new LogicException($"{tableName}는 키가 존재하지 않는 테이블입니다.", value.Tracker);

            if (string.IsNullOrEmpty(columnName) == false)
            {
                if (Context.Completed.Schema.ContainsColumn(tableName, columnName) == false)
                    throw new LogicException($"{tableName}에 {columnName} 컬럼이 존재하지 않습니다.", value.Tracker);
            }

            yield return true;
        }

        protected override void OnError(RelationTypeValidationData input, Exception e, IExcelFileTrackable tracker = null)
        {
            base.OnError(input, e, tracker);
        }

        protected override void OnWorked(RelationTypeValidationData input, bool output, int percent)
        {
            Logger.Write($"참조 타입을 검사했습니다. - {input.Type}");
        }

        protected override IReadOnlyList<bool> OnFinish(IReadOnlyList<bool> output)
        {
            Logger.Complete($"참조 타입을 검사했습니다.");
            return base.OnFinish(output);
        }
    }
}
