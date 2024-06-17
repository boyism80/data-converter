using ExcelTableConverter.Model;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.CS
{
    public class EnumFileGenerator : ParallelWorker<string, bool>
    {
        private static readonly Template _template = Template.Parse(File.ReadAllText($"Template/C#/enum.txt"));
        private readonly string _dir;

        public EnumFileGenerator(Context ctx) : base(ctx)
        {
            _dir = Path.Combine(ctx.Output, Context.Config.EnumCodeFilePath);
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

        protected override IEnumerable<string> OnReady()
        {
            foreach (var enumName in Context.Result.Enum.Keys)
            {
                yield return enumName;
            }
        }

        protected override IEnumerable<bool> OnWork(string enumName)
        {
            var code = _template.Render(new { Namespaces = Context.Config.Namespace, Name = enumName, Properties = Context.Result.Enum[enumName].OrderBy(x => x, new Util.Enum.Comparer()) });
            var path = Path.Combine(_dir, $"{enumName}.cs");
            File.WriteAllText(path, code);
            yield return true;
        }

        protected override void OnWorked(string input, bool output, int percent)
        {
            Logger.Write($"열거형 코드 파일을 생성했습니다. - {input}", percent: percent);
            base.OnWorked(input, output, percent);
        }

        protected override IReadOnlyList<bool> OnFinish(IReadOnlyList<bool> output)
        {
            Logger.Complete("열거형 코드 파일을 생성했습니다.");
            return base.OnFinish(output);
        }
    }
}
