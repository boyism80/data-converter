using ExcelTableConverter.Factory.CS;
using ExcelTableConverter.Model;
using ExcelTableConverter.Model.CS;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.CS
{
    public class ClassFileGenerator : ParallelWorker<string, bool>
    {
        private static readonly Template _template = Template.Parse(File.ReadAllText("Template/C#/class.txt"));
        private readonly string _dir;

        public ClassFileGenerator(Context ctx) : base(ctx)
        {
            _dir = Path.Combine(ctx.Output, Context.Config.ClassCodeFilePath);
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

            foreach (var scope in new[] { Scope.Common, Scope.Server, Scope.Client })
            {
                var dir = Path.Combine(_dir, $"{scope}".ToLower());
                if (Directory.Exists(dir) == false)
                    Directory.CreateDirectory(dir);
            }
        }

        protected override IEnumerable<string> OnReady()
        {
            foreach (var tableName in Context.Result.Schema.Keys)
            {
                yield return tableName;
            }
        }

        protected override IEnumerable<bool> OnWork(string tableName)
        {
            var schemaSet = Context.Result.Schema[tableName];
            var result = new[] { Scope.Server, Scope.Client, Scope.Common }.ToDictionary(x => x, x => new List<ClassCodeGenerationProperty>());
            var properties = schemaSet.Values.ToList();
            for (int i = 0; i < properties.Count; i++)
            {
                var property = properties[i];

                result[property.Scope].Add(new ClassCodeGenerationProperty
                {
                    Index = i,
                    Key = Util.Type.IsKey(property.Type, out _),
                    Type = new TypeFactory(Context).Build(property.Type),
                    Name = property.Name
                });
            }

            foreach (var (scope, p) in result)
            {
                var super = scope == Scope.Common;
                var inherit = !super && result[Scope.Common].Count > 0;
                if (!inherit && p.Count == 0)
                    continue;

                var code = _template.Render(new { Namespaces = Context.Config.Namespace, Scope = scope, Name = tableName, Properties = p, Super = super, Inherit = inherit });
                var path = Path.Combine(_dir, $"{scope}".ToLower(), $"{tableName}.cs");
                File.WriteAllText(path, code);
            }

            yield return true;
        }

        protected override void OnWorked(string input, bool output, int percent)
        {
            Logger.Write($"클래스 코드 파일을 저장했습니다. - {input}", percent: percent);
        }

        protected override IReadOnlyList<bool> OnFinish(IReadOnlyList<bool> output)
        {
            Logger.Complete($"클래스 코드 파일을 저장했습니다.");
            return base.OnFinish(output);
        }
    }
}
