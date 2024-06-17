using ExcelTableConverter.Model;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.Node
{
    public class EnumCodeGenerator : ParallelWorker<string, (string Name, List<KeyValuePair<string, List<object>>> Props)>
    {
        private static readonly Template _template = Template.Parse(File.ReadAllText($"Template/Node/enum.txt"));

        public string Result { get; private set; }

        public EnumCodeGenerator(Context ctx) : base(ctx)
        {
        }

        protected override IEnumerable<string> OnReady()
        {
            foreach (var enumName in Context.Result.Enum.Keys)
            {
                yield return enumName;
            }
        }

        protected override IEnumerable<(string Name, List<KeyValuePair<string, List<object>>> Props)> OnWork(string value)
        {
            yield return (value, Context.Result.Enum[value].OrderBy(x => x, new Util.Enum.Comparer()).ToList());
        }

        protected override void OnWorked(string input, (string Name, List<KeyValuePair<string, List<object>>> Props) output, int percent)
        {
            base.OnWorked(input, output, percent);
        }

        protected override IReadOnlyList<(string Name, List<KeyValuePair<string, List<object>>> Props)> OnFinish(IReadOnlyList<(string Name, List<KeyValuePair<string, List<object>>> Props)> output)
        {
            Result = _template.Render(new
            {
                Items = output.OrderBy(x => x.Name).Select(x => new
                {
                    x.Name,
                    Props = x.Props.Select(x => new
                    {
                        Name = x.Key,
                        Value = x.Value
                    }).ToList()
                }).ToList()
            });

            return base.OnFinish(output);
        }
    }
}
