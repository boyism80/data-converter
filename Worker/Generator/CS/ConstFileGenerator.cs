using ExcelTableConverter.Factory.CS;
using ExcelTableConverter.Model;
using ExcelTableConverter.Model.CS;
using Scriban;
using System.Text;

namespace ExcelTableConverter.Worker.Generator.CS
{
    public class ConstFileGenerator : ParallelWorker<Scope, bool>
    {
        private static readonly Template _template = Template.Parse(File.ReadAllText($"Template/C#/const.txt"));
        private readonly string _dir;

        public ConstFileGenerator(Context ctx) : base(ctx)
        {
            _dir = Path.Combine(ctx.Output, Context.Config.ConstCodeFilePath);
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

        protected override IEnumerable<Scope> OnReady()
        {
            foreach (var scope in new[] { Scope.Common, Scope.Server, Scope.Client })
            {
                var dir = Path.Combine(_dir, $"{scope}".ToLower());
                if (Directory.Exists(dir) == false)
                    Directory.CreateDirectory(dir);

                foreach (var file in Directory.GetFiles(dir))
                    File.Delete(file);

                yield return scope;
            }
        }

        protected override IEnumerable<bool> OnWork(Scope scope)
        {
            var items = new Dictionary<string, List<ConstCodeGeneratorProperty>>();
            foreach (var (groupName, constSet) in Context.Result.Const.OrderBy(x => x.Key))
            {
                var properties = new List<ConstCodeGeneratorProperty>();
                foreach (var constData in constSet.Values)
                {
                    if (constData.Scope == scope)
                    {
                        properties.Add(new ConstCodeGeneratorProperty
                        {
                            Name = constData.Name,
                            Type = new TypeFactory(Context).Build(constData.Type),
                            Value = new AllocateValueFactory(Context).Build(constData.Type, constData.Value),
                        });
                    }
                }

                items.Add(groupName, properties);
            }

            var code = _template.Render(new { Namespaces = Context.Config.Namespace, Super = scope == Scope.Common, Scope = scope, Items = items });
            var path = Path.Combine(_dir, $"{scope}".ToLower(), "Const.cs");
            File.WriteAllText(path, code, Encoding.UTF8);
            yield return true;
        }

        protected override void OnWorked(Scope input, bool output, int percent)
        {
            Logger.Write($"상수 코드 파일을 생성했습니다. - {input}", percent: percent);
            base.OnWorked(input, output, percent);
        }

        protected override IReadOnlyList<bool> OnFinish(IReadOnlyList<bool> output)
        {
            Logger.Complete("상수 코드 파일을 생성했습니다.");
            return base.OnFinish(output);
        }
    }
}
