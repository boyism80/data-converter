using ExcelTableConverter.Factory.CPP;
using ExcelTableConverter.Model;
using ExcelTableConverter.Model.CPP;
using Newtonsoft.Json;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.CPP
{
    public class DslCodeGenerator : ParallelWorker<KeyValuePair<string, List<DSLParameter>>, (string Name, List<DslCodeGeneratorProperty> Props)>
    {
        private static readonly Dictionary<string, List<DSLParameter>> _prototypes = JsonConvert.DeserializeObject<Dictionary<string, List<DSLParameter>>>(File.ReadAllText("dsl.json"));
        private readonly string _dir;

        public string Result { get; private set; }

        public DslCodeGenerator(Context ctx) : base(ctx)
        {
            _dir = Path.Combine(ctx.Output, Context.Config.DslCodeFilePath);
            if (Directory.Exists(_dir) == false)
                Directory.CreateDirectory(_dir);

            foreach (var file in Directory.GetFiles(_dir))
                File.Delete(file);
        }

        protected override IEnumerable<KeyValuePair<string, List<DSLParameter>>> OnReady()
        {
            foreach (var pair in _prototypes)
                yield return pair;
        }

        protected override IEnumerable<(string Name, List<DslCodeGeneratorProperty> Props)> OnWork(KeyValuePair<string, List<DSLParameter>> value)
        {
            var name = value.Key;
            var props = value.Value.Select((prototype, i) =>
            {
                return new DslCodeGeneratorProperty
                {
                    Name = prototype.Name,
                    Type = new TypeFactory(Context).Build(prototype.Type),
                    RType = new TypeFactory(Context).Build(prototype.Type, true),
                    Serialize = Context.GetCSharpSerializeCode(prototype.Type, prototype.Name),
                    Deserialize = new ValueDeserializeFactory(Context).Build(prototype.Type, $"parameters[{i}]")
                };
            }).ToList();

            yield return (name, props);
        }

        protected override void OnWorked(KeyValuePair<string, List<DSLParameter>> input, (string Name, List<DslCodeGeneratorProperty> Props) output, int percent)
        {
            base.OnWorked(input, output, percent);
        }

        protected override IReadOnlyList<(string Name, List<DslCodeGeneratorProperty> Props)> OnFinish(IReadOnlyList<(string Name, List<DslCodeGeneratorProperty> Props)> output)
        {
            var template = Template.Parse(File.ReadAllText($"Template/C++/dsl.txt"));
            var parameters = new 
            {
                Namespace = Util.CPP.Namespace.Access(Context.Config.Namespace), 
                Items = output.OrderBy(x => x.Name).Select(x => new { x.Name, x.Props }),
                Dsls = _prototypes.Keys.OrderBy(x => x).ToList() 
            };

            Result = template.Render(parameters);
            return base.OnFinish(output);
        }
    }
}
