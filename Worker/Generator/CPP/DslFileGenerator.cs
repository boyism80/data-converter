using ExcelTableConverter.Factory.CPP;
using ExcelTableConverter.Model;
using ExcelTableConverter.Model.CPP;
using Newtonsoft.Json;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.CPP
{
    public class DslFileGenerator : ParallelWorker<KeyValuePair<string, List<DSLParameter>>, (string Name, string Header, string Source)>
    {
        private static readonly Template _headerTemplate = Template.Parse(File.ReadAllText($"Template/C++/dsl.header.txt"));
        private static readonly Template _sourceTemplate = Template.Parse(File.ReadAllText($"Template/C++/dsl.source.txt"));
        private static readonly Dictionary<string, List<DSLParameter>> _prototypes = JsonConvert.DeserializeObject<Dictionary<string, List<DSLParameter>>>(File.ReadAllText("dsl.json"));
        private readonly string _dir;

        public DslFileGenerator(Context ctx) : base(ctx)
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

        protected override IEnumerable<(string, string, string)> OnWork(KeyValuePair<string, List<DSLParameter>> value)
        {
            var name = value.Key;
            var prototypes = value.Value;

            var properties = prototypes.Select((prototype, i) =>
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

            var header = _headerTemplate.Render(new { Name = name, Params = properties });
            var source = _sourceTemplate.Render(new { Name = name, Params = properties });
            yield return (name, header, source);
        }

        protected override void OnWorked(KeyValuePair<string, List<DSLParameter>> input, (string, string, string) output, int percent)
        {
            Logger.Write($"DSL 파일을 생성했습니다. - {input.Key}", percent: percent);
            base.OnWorked(input, output, percent);
        }

        protected override IReadOnlyList<(string Name, string Header, string Source)> OnFinish(IReadOnlyList<(string Name, string Header, string Source)> output)
        {
            var codes = output.OrderBy(x => x.Name).Select(x => (x.Header, x.Source)).ToList();
            var template = Template.Parse(File.ReadAllText($"Template/C++/dsl.txt"));
            var parameters = new 
            {
                Headers = codes.Select(x => x.Header).ToList(), 
                Sources = codes.Select(x => x.Source).ToList(), 
                Dsls = _prototypes.Keys.OrderBy(x => x).ToList() 
            };
            File.WriteAllText(Path.Combine(_dir, $"dsl.h"), template.Render(parameters));

            Logger.Complete("DSL 파일을 생성했습니다.");
            return base.OnFinish(output);
        }
    }
}
