using ExcelTableConverter.Model;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.CPP
{
    public class EnumFileGenerator : ParallelWorker<string, (string Name, string Code)>
    {
        private static readonly Template _template = Template.Parse(File.ReadAllText($"Template/C++/enum.txt"));
        private readonly string _dir;

        public EnumFileGenerator(Context ctx) : base(ctx)
        {
            _dir = Path.Combine(ctx.Output, Context.Config.EnumCodeFilePath);
            if (Directory.Exists(_dir) == false)
                Directory.CreateDirectory(_dir);

            foreach (var file in Directory.GetFiles(_dir))
                File.Delete(file);
        }

        protected override IEnumerable<string> OnReady()
        {
            foreach (var enumName in Context.Result.Enum.Keys)
            {
                yield return enumName;
            }
        }

        protected override IEnumerable<(string, string)> OnWork(string enumName)
        {
            var code = _template.Render(new { Name = enumName, Properties = Context.Result.Enum[enumName].OrderBy(x => x.Value) });
            yield return (enumName, code);
        }

        protected override void OnWorked(string input, (string Name, string Code) output, int percent)
        {
            Logger.Write($"열거형 코드 파일을 생성했습니다. - {input}", percent: percent);
            base.OnWorked(input, output, percent);
        }

        protected override IReadOnlyList<(string Name, string Code)> OnFinish(IReadOnlyList<(string Name, string Code)> output)
        {
            var codeList = output.OrderBy(x => x.Name).Select(x => x.Code).ToList();
            var template = Template.Parse(File.ReadAllText($"Template/C++/enum.complete.txt"));
            var code = template.Render(new { Codes = codeList });
            var path = Path.Combine(_dir, $"enum.h");
            File.WriteAllText(path, code);

            Logger.Complete("열거형 코드 파일을 생성했습니다.");
            return base.OnFinish(output);
        }
    }
}
