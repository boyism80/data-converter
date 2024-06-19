using ExcelTableConverter.Model;
using System.Collections.Concurrent;

namespace ExcelTableConverter.Worker.Validator
{
    public class RelationValueValidationData
    { 
        public IExcelFileTrackable Tracker { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public object Value { get; set; }
        public Scope Scope { get; set; }
    }

    public class RelationValueValidator : ParallelWorker<RelationValueValidationData[], bool>
    {
        private readonly ConcurrentDictionary<(string Table, string Column), HashSet<string>> _refs = new ConcurrentDictionary<(string Table, string Column), HashSet<string>>();
        private readonly IEnumerable<RelationValueValidationData> _rvds;
        private const int CHUNK_SIZE = 1000;

        public RelationValueValidator(Context ctx, IEnumerable<RelationValueValidationData> rvds) : base(ctx)
        {
            _rvds = rvds;
        }

        protected override IEnumerable<RelationValueValidationData[]> OnReady()
        {
            foreach (var chunk in _rvds.Chunk(CHUNK_SIZE))
            {
                yield return chunk;
            }
        }

        protected override IEnumerable<bool> OnWork(RelationValueValidationData[] values)
        {
            var errors = new List<Exception>();
            foreach (var value in values)
            {
                try
                {
                    var refer = Util.Type.Nake(value.Type);
                    if (Context.SplitReferenceType(refer, out var tableName, out var columnName) == false)
                        throw new LogicException("알 수 없는 에러");

                    if (columnName == null)
                        columnName = Context.GetKey(tableName)?.Name ?? throw new LogicException($"{tableName}은 키가 정의되지 않은 테이블입니다.", value.Tracker);

                    if (Context.ContainsColumn(tableName, columnName) == false)
                        throw new LogicException($"{columnName}은 {tableName}의 멤버가 아닙니다.", value.Tracker);

                    var hash = _refs.GetOrAdd((tableName, columnName), _ => Context.GetValuesFromJson(tableName, columnName).Select(x => $"{x}").ToHashSet());

                    if (hash.Contains($"{value.Value}") == false)
                        throw new LogicException($"{value.Name}의 값 '{value.Value}'는 {refer} 테이블에 존재하지 않습니다.", value.Tracker);
                }
                catch (Exception e)
                {
                    errors.Add(e);
                }
            }

            if (errors.Count > 0)
                throw new AggregateException(errors);

            yield return true;
        }

        protected override void OnWorked(RelationValueValidationData[] input, bool output, int percent)
        {
            Logger.Write($"참조 타입 데이터 유효성을 검사했습니다. - {input[0].Tracker.SheetName}", percent: percent);
        }

        protected override IReadOnlyList<bool> OnFinish(IReadOnlyList<bool> output)
        {
            Logger.Complete($"참조 타입 데이터 유효성을 검사했습니다.");
            return base.OnFinish(output);
        }

        protected override void OnError(RelationValueValidationData[] input, Exception e, IExcelFileTrackable tracker = null)
        {
            base.OnError(input, e, tracker);
        }
    }
}
