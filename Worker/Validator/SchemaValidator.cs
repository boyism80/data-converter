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
            var pivot = value.First();
            foreach (var rsd in value)
            {
                if (pivot.Schema.SequenceEqual(rsd.Schema) == false)
                    throw new LogicException($"{pivot.Root}와 {rsd.Root}의 스키마를 병합할 수 없습니다.", rsd);
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
