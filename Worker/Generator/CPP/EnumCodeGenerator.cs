using ExcelTableConverter.Model;
using ExcelTableConverter.Util;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.CPP
{
    public class EnumCodeGeneratorResult
    {
        public string Name { get; set; }
        public object Props { get; set; }
    }

    public class EnumCodeGenerator : ParallelWorker<string, EnumCodeGeneratorResult>
    {
        private static readonly Template _declarationTemplate = Template.Parse(File.ReadAllText($"Template/C++/enum.txt"));

        public string Declaration { get; private set; }
        public List<object> Enums { get; private set; }

        public EnumCodeGenerator(Context ctx) : base(ctx)
        {
        }

        private static string GenerateDeclarationCode(List<object> enums)
        {
            var obj = new ScribanEx
            {
                ["enums"] = enums,
                ["config"] = Context.Config,
            };
            var ctx = new TemplateContext();
            ctx.PushGlobal(obj);
            return _declarationTemplate.Render(ctx);
        }

        protected override IEnumerable<string> OnReady()
        {
            foreach (var enumName in Context.Result.Enum.Keys)
            {
                yield return enumName;
            }
        }

        protected override IEnumerable<EnumCodeGeneratorResult> OnWork(string enumName)
        {
            var props = Context.Result.Enum[enumName].OrderBy(x => x, new Util.Enum.Comparer()).Select(x => new
            {
                Name = x.Key,
                Value = x.Value
            } as object).ToList();
            yield return new EnumCodeGeneratorResult
            {
                Name = enumName,
                Props = props
            };
        }

        protected override void OnWorked(string input, EnumCodeGeneratorResult output, int percent)
        {
            base.OnWorked(input, output, percent);
        }

        protected override IReadOnlyList<EnumCodeGeneratorResult> OnFinish(IReadOnlyList<EnumCodeGeneratorResult> output)
        {
            Enums = output.OrderBy(x => x.Name).Select(x => new
            {
                x.Name,
                x.Props
            } as object).ToList();

            Declaration = GenerateDeclarationCode(Enums);

            Logger.Complete("열거형 코드 파일을 생성했습니다.");
            return base.OnFinish(output);
        }
    }
}
