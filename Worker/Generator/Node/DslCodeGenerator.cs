using ExcelTableConverter.Factory.Node;
using ExcelTableConverter.Model;
using Newtonsoft.Json;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.Node
{
    public class DslCodeGenerator : ParallelWorker<KeyValuePair<string, List<DSLParameter>>, (string Name, List<object> Props)>
    {
        private static readonly Dictionary<string, List<DSLParameter>> _prototypes = JsonConvert.DeserializeObject<Dictionary<string, List<DSLParameter>>>(Context.DSL.ToString());

        public string Result { get; private set; }

        public DslCodeGenerator(Context ctx) : base(ctx)
        {
        }

        protected override IEnumerable<KeyValuePair<string, List<DSLParameter>>> OnReady()
        {
            foreach (var pair in _prototypes)
                yield return pair;
        }

        protected override IEnumerable<(string Name, List<object> Props)> OnWork(KeyValuePair<string, List<DSLParameter>> value)
        {
            var name = value.Key;
            var props = value.Value.Select((prototype, i) =>
            {
                return new
                {
                    Name = prototype.Name,
                    Initializer = new TypeBuilderFactory(Context).Build(prototype.Type)
                } as object;
            }).ToList();

            yield return (name, props);
        }

        protected override void OnWorked(KeyValuePair<string, List<DSLParameter>> input, (string Name, List<object> Props) output, int percent)
        {
            base.OnWorked(input, output, percent);
        }

        protected override IReadOnlyList<(string Name, List<object> Props)> OnFinish(IReadOnlyList<(string Name, List<object> Props)> output)
        {
            var template = Template.Parse(File.ReadAllText($"Template/Node/dsl.txt"));
            var parameters = new
            {
                Items = output.OrderBy(x => x.Name).Select(x => new { x.Name, x.Props })
            };

            Result = template.Render(parameters);
            return base.OnFinish(output);
        }
    }
}
