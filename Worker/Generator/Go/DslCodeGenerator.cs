using ExcelTableConverter.Factory.Go;
using ExcelTableConverter.Model;
using ExcelTableConverter.Util;
using Newtonsoft.Json;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.Go
{
    public class DslCodeGeneratorResult
    {
        public string DslFunctionType { get; set; }
        public string Header { get; set; }
        public List<object> Props { get; set; }
    }

    public class DslCodeGenerator : ParallelWorker<KeyValuePair<string, List<DSLParameter>>, DslCodeGeneratorResult>
    {
        private readonly Dictionary<string, List<DSLParameter>> _prototypes;
        public string Result { get; private set; }

        public DslCodeGenerator(Context ctx) : base(ctx)
        {
            _prototypes = JsonConvert.DeserializeObject<Dictionary<string, List<DSLParameter>>>(ctx.DSL.ToString());
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
                    Name = prototype.Name,
                    Type = new TypeFactory(Context).Build(prototype.Type),
                    //Serialize = GetCSharpSerializeCode(prototype.Type, prototype.Name),
                    Deserialize = $"params[{i}].({new TypeFactory(Context).Build(prototype.Type)})"
                } as object;
            }).ToList();

            yield return new DslCodeGeneratorResult
            {
                DslFunctionType = Context.Configuration.DslTypeEnumName,
                Header = header,
                Props = props
            };
        }

        protected override void OnWorked(KeyValuePair<string, List<DSLParameter>> input, DslCodeGeneratorResult output, int percent)
        {
            Logger.Write($"DSL 파일을 생성했습니다. - {input.Key}".AsSpan());
            base.OnWorked(input, output, percent);
        }

        protected override IReadOnlyList<DslCodeGeneratorResult> OnFinish(IReadOnlyList<DslCodeGeneratorResult> output)
        {
            var template = Template.Parse(File.ReadAllText($"Template/Go/dsl.txt"));
            var obj = new ScribanEx
            {
                ["items"] = output.OrderBy(x => x.Header).ToList(),
                ["dsls"] = _prototypes.Keys.OrderBy(x => x).ToList(),
                ["config"] = Context.Configuration,
            };

            var ctx = new TemplateContext();
            ctx.PushGlobal(obj);
            Result = template.Render(ctx);

            Logger.Complete("DSL 파일을 생성했습니다.");
            return base.OnFinish(output);
        }
    }
}
