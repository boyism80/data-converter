using ExcelTableConverter.Model;

namespace ExcelTableConverter.Worker.Validator
{
    public class EnumValidator : ParallelWorker<SourceEnum, bool>
    {
        private readonly Dictionary<string, Dictionary<string, EnumExpression>> _merge = new Dictionary<string, Dictionary<string, EnumExpression>>();

        public EnumValidator(Context ctx) : base(ctx)
        {

        }

        protected override IEnumerable<SourceEnum> OnReady()
        {
            foreach (var g in Context.Source.Enum.SelectMany(x => x.Value).GroupBy(x => x.Table))
            {
                var table = g.Key;
                var merge = new Dictionary<string, EnumExpression>();
                foreach (var x in g.Select(x => x.Values))
                {
                    foreach (var (k, v) in x)
                        merge.Add(k, v);
                }

                _merge.Add(table, merge);
            }

            foreach (var sources in Context.Source.Enum.Values)
            {
                foreach (var source in sources)
                    yield return source;
            }
        }

        protected override IEnumerable<bool> OnWork(SourceEnum value)
        {
            foreach (var expression in value.Values.Values)
            {
                expression.Validate(value.Table, _merge[value.Table], value);
            }

            yield return true;
        }

        protected override void OnWorked(SourceEnum input, bool output, int percent)
        {
            Logger.Write("열거형 구문을 검사중입니다.");
            base.OnWorked(input, output, percent);
        }

        protected override IReadOnlyList<bool> OnFinish(IReadOnlyList<bool> output)
        {
            Logger.Complete("열거형 구문을 검사했습니다.");
            return base.OnFinish(output);
        }
    }
}
