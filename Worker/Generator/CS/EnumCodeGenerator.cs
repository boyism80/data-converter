using ExcelTableConverter.Model;
using ExcelTableConverter.Util;
using Scriban;
using static System.Formats.Asn1.AsnWriter;

namespace ExcelTableConverter.Worker.Generator.CS
{
    public class EnumCodeGeneratorResult
    {
        public string Name { get; set; }
        public object Props { get; set; }
    }

    public class EnumCodeGenerator : ParallelWorker<string, EnumCodeGeneratorResult>
    {
        private static readonly Template _template = Template.Parse(File.ReadAllText($"Template/C#/enum.txt"));
        private readonly string _dir;

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

        protected override IEnumerable<EnumCodeGeneratorResult> OnWork(string enumName)
        {
            var props = Context.Result.Enum[enumName].OrderBy(x => x, new Util.Enum.Comparer()).Select(x => new
            {
                Name = x.Key,
                Value = x.Value.Select(x => 
                {
                    if (x is string s)
                        return s;
                    else
                        return x;
                }).ToList()
            } as object).ToList();

            if (props.Count == 0)
                yield break;


            yield return new EnumCodeGeneratorResult
            {
                Name = enumName,
                Props = props
            };
        }

        protected override void OnWorked(string input, EnumCodeGeneratorResult output, int percent)
        {
            Logger.Write($"열거형 코드 파일을 생성했습니다. - {input}", percent: percent);
            base.OnWorked(input, output, percent);
        }

        protected override IReadOnlyList<EnumCodeGeneratorResult> OnFinish(IReadOnlyList<EnumCodeGeneratorResult> output)
        {
            var items = output.OrderBy(x => x.Name).Select(x => new
            {
                x.Name,
                x.Props
            } as object).ToList();

            var obj = new ScribanEx();
            obj.Add("items", items);
            obj.Add("config", Context.Config);

            var ctx = new TemplateContext();
            ctx.PushGlobal(obj);

            Result = _template.Render(ctx);

            Logger.Complete("열거형 코드 파일을 생성했습니다.");
            return base.OnFinish(output);
        }
    }
}
