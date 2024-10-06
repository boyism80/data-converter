using ExcelTableConverter.Factory.CPP;
using ExcelTableConverter.Model;
using ExcelTableConverter.Util;
using Newtonsoft.Json;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.CPP
{
    public class DslCodeGeneratorResult
    { 
        public string Name { get; set; }
        public List<object> Props { get; set; }
    }

    public class DslCodeGenerator : ParallelWorker<KeyValuePair<string, List<DSLParameter>>, DslCodeGeneratorResult>
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

        protected override IEnumerable<DslCodeGeneratorResult> OnWork(KeyValuePair<string, List<DSLParameter>> value)
        {
            var name = value.Key;
            var props = value.Value.Select((prototype, i) =>
            {
                return new
                {
                    Name = prototype.Name,
                    Type = new TypeFactory(Context).Build(prototype.Type),
                } as object;
            }).ToList();

            yield return new DslCodeGeneratorResult
            {
                Name = name,
                Props = props
            };
        }

        protected override void OnWorked(KeyValuePair<string, List<DSLParameter>> input, DslCodeGeneratorResult output, int percent)
        {
            base.OnWorked(input, output, percent);
        }

        protected override IReadOnlyList<DslCodeGeneratorResult> OnFinish(IReadOnlyList<DslCodeGeneratorResult> output)
        {
            var template = Template.Parse(File.ReadAllText($"Template/C++/dsl.txt"));
            var obj = new ScribanEx();
            var ctx = new TemplateContext();

            obj.Add("items", output.OrderBy(x => x.Name).Select(x => new { x.Name, x.Props }));
            obj.Add("dsls", _prototypes.Keys.OrderBy(x => x).ToList());
            obj.Add("config", Context.Config);
            ctx.PushGlobal(obj);
            Result = template.Render(ctx);
            return base.OnFinish(output);
        }
    }
}
