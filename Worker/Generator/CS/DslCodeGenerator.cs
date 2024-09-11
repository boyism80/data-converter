using ExcelTableConverter.Factory.CS;
using ExcelTableConverter.Model;
using ExcelTableConverter.Util;
using Newtonsoft.Json;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.CS
{
    public class DslCodeGeneratorResult
    {
        public string DslFunctionType { get; set; }
        public string Header { get; set; }
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
            var header = value.Key;
            var prototypes = value.Value;

            var props = prototypes.Select((prototype, i) =>
            {
                return new
                {
                    Name = prototype.Name.ToCamelCase(),
                    Type = new TypeFactory(Context).Build(prototype.Type),
                    Serialize = Context.GetCSharpSerializeCode(prototype.Type, prototype.Name),
                    Deserialize = new ValueDeserializeFactory(Context).Build(prototype.Type, $"parameters[{i}]")
                } as object;
            }).ToList();

            yield return new DslCodeGeneratorResult
            {
                DslFunctionType = Context.Config.DslTypeEnumName,
                Header = header.ToCamelCase(),
                Props = props
            };
        }

        protected override void OnWorked(KeyValuePair<string, List<DSLParameter>> input, DslCodeGeneratorResult output, int percent)
        {
            Logger.Write($"DSL 파일을 생성했습니다. - {input.Key}", percent: percent);
            base.OnWorked(input, output, percent);
        }

        protected override IReadOnlyList<DslCodeGeneratorResult> OnFinish(IReadOnlyList<DslCodeGeneratorResult> output)
        {
            var template = Template.Parse(File.ReadAllText($"Template/C#/dsl.txt"));
            var parameters = new 
            {
                Namespace = Context.Config.Namespace.ConvertAll(x => x.ToCamelCase()),
                EnumNamespace = Context.Config.EnumNamespace.ConvertAll(x => x.ToCamelCase()),
                DslFunctionType = Context.Config.DslTypeEnumName.ToCamelCase(),
                Items = output.OrderBy(x => x.Header),
                Dsls = _prototypes.Keys.OrderBy(x => x).ToList() 
            };

            Result = template.Render(parameters);

            Logger.Complete("DSL 파일을 생성했습니다.");
            return base.OnFinish(output);
        }
    }
}
