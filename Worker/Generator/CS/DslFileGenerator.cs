using ExcelTableConverter.Factory.CS;
using ExcelTableConverter.Model;
using Newtonsoft.Json;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.CS
{
    public class DslFileGenerator : ParallelWorker<KeyValuePair<string, List<DSLParameter>>, bool>
    {
        private static readonly Template _template = Template.Parse(File.ReadAllText($"Template/C#/dsl.txt"));
        private static readonly Dictionary<string, List<DSLParameter>> _prototypes = JsonConvert.DeserializeObject<Dictionary<string, List<DSLParameter>>>(File.ReadAllText("dsl.json"));
        private readonly string _dir;

        public DslFileGenerator(Context ctx) : base(ctx)
        {
            _dir = Path.Combine(ctx.Output, Context.Config.DslCodeFilePath);
            if (Directory.Exists(_dir) == false)
                Directory.CreateDirectory(_dir);

            foreach (var file in Directory.GetFiles(_dir))
            {
                File.Delete(file);
            }

            foreach (var dir in Directory.GetDirectories(_dir))
            {
                Directory.Delete(dir, true);
            }
        }

        protected override IEnumerable<KeyValuePair<string, List<DSLParameter>>> OnReady()
        {
            foreach (var pair in _prototypes)
                yield return pair;
        }

        protected override IEnumerable<bool> OnWork(KeyValuePair<string, List<DSLParameter>> value)
        {
            var header = value.Key;
            var prototypes = value.Value;

            var props = prototypes.Select((prototype, i) =>
            {
                return new
                {
                    Name = prototype.Name,
                    Type = new TypeFactory(Context).Build(prototype.Type),
                    Serialize = Context.GetCSharpSerializeCode(prototype.Type, prototype.Name),
                    Deserialize = new ValueDeserializeFactory(Context).Build(prototype.Type, $"parameters[{i}]")
                } as object;
            }).ToList();

            var code = _template.Render(new { Namespaces = Context.Config.Namespace, Header = header, Props = props });
            var path = Path.Combine(_dir, $"{header}.cs");
            File.WriteAllText(path, code);
            yield return true;
        }

        protected override void OnWorked(KeyValuePair<string, List<DSLParameter>> input, bool output, int percent)
        {
            Logger.Write($"DSL 파일을 생성했습니다. - {input.Key}", percent: percent);
            base.OnWorked(input, output, percent);
        }

        protected override IReadOnlyList<bool> OnFinish(IReadOnlyList<bool> output)
        {
            Logger.Complete("DSL 파일을 생성했습니다.");
            return base.OnFinish(output);
        }
    }
}
