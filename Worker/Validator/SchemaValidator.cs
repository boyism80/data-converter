using ExcelTableConverter.Model;

namespace ExcelTableConverter.Worker.Validator
{
    public class SchemaValidator : ParallelWorker<List<RawSheetData>, bool>
    {
        public SchemaValidator(Context ctx) : base(ctx)
        {
            
        }

        protected override IEnumerable<List<RawSheetData>> OnReady()
        {
            foreach (var g in Context.RawData.SelectMany(x => x.Value).GroupBy(x => x.TableName))
            {
                yield return g.ToList();
            }
        }

        protected override IEnumerable<bool> OnWork(List<RawSheetData> value)
        {
            var basedSet = new Dictionary<string, IExcelFileTrackable>();
            foreach (var rsd in value)
            {
                var based = rsd.Based ?? string.Empty;
                if (basedSet.ContainsKey(based))
                    continue;

                basedSet.Add(based, rsd);
                if (basedSet.Count > 1)
                {
                    var trace = string.Join(", ", basedSet.Select(x => $"{x.Key}({x.Value.FileName}:{x.Value.SheetName})"));
                    throw new LogicException($"{rsd.TableName} 테이블이 서로 다른 테이블을 상속받고 있습니다. - {trace}");
                }
            }

            var pivot = value.First();
            foreach (var rsd in value)
            {
                if (pivot.Schema.SequenceEqual(rsd.Schema) == false)
                    throw new LogicException($"{pivot.Root}와 {rsd.Root}의 스키마를 병합할 수 없습니다.", rsd);
            }

            if (string.IsNullOrEmpty(pivot.Based) == false)
            {
                var basedSheet = Context.RawData.SelectMany(x => x.Value).FirstOrDefault(x => x.TableName == pivot.Based) ??
                    throw new LogicException($"{pivot.Based}는 존재하지 않는 테이블입니다.", pivot);

                foreach (var (name, type, scope) in basedSheet.Schema.Select(x => (Name: x.Name, Type: x.Type, Scope: x.Scope)))
                {
                    var inherited = pivot.Schema.FirstOrDefault(x => x.Name == name) ??
                        throw new LogicException($"{pivot.TableName} 테이블에 {basedSheet.TableName} 테이블의 {name} 컬럼이 정의되지 않았습니다.", pivot);

                    if (inherited.Type != type)
                        throw new LogicException($"{inherited.Name}의 타입({inherited.Type})이 {basedSheet.TableName}에 정의된 타입({type})과 다릅니다.", pivot);

                    if (inherited.Scope.HasFlag(scope) == false)
                        throw new LogicException($"{inherited.Name}의 스코프({inherited.Scope})가 {basedSheet.TableName}에 정의된 스코프({scope})에 포함되지 않습니다.", pivot);
                }
            }
            yield return true;
        }

        protected override void OnWorked(List<RawSheetData> input, bool output, int percent)
        {
            Logger.Write($"스키마 병합 가능 여부를 검사했습니다. - {input[0].TableName}", percent: percent);
        }

        protected override IReadOnlyList<bool> OnFinish(IReadOnlyList<bool> output)
        {
            Logger.Complete($"스키마 병합 가능 여부를 검사했습니다.");
            return base.OnFinish(output);
        }

        protected override void OnError(List<RawSheetData> input, Exception e, IExcelFileTrackable tracker = null)
        {
            base.OnError(input, e, tracker);
        }
    }
}
