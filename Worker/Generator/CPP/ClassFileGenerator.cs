using ExcelTableConverter.Factory.CPP;
using ExcelTableConverter.Model;
using ExcelTableConverter.Model.CPP;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.CPP
{
    public class ClassFileGenerator : ParallelWorker<string, (Scope Scope, string Name, string Code)>
    {
        private static readonly Template _template = Template.Parse(File.ReadAllText("Template/C++/class.txt"));
        private readonly string _dir;

        public ClassFileGenerator(Context ctx) : base(ctx)
        {
            _dir = Path.Combine(ctx.Output, Context.Config.ClassCodeFilePath);

            foreach (var scope in new[] { Scope.Common, Scope.Server, Scope.Client })
            {
                var dir = Path.Combine(_dir, $"{scope}".ToLower());
                if (Directory.Exists(dir) == false)
                    Directory.CreateDirectory(dir);

                foreach (var file in Directory.GetFiles(dir))
                    File.Delete(file);
            }
        }

        protected override IEnumerable<string> OnReady()
        {
            foreach (var tableName in Context.Result.Schema.Keys)
            {
                yield return tableName;
            }
        }

        protected override IEnumerable<(Scope, string, string)> OnWork(string tableName)
        {
            var schemaSet = Context.Result.Schema[tableName];
            var result = new[] { Scope.Server, Scope.Client, Scope.Common }.ToDictionary(x => x, x => new List<ClassCodeGenerationProperty>());
            var properties = schemaSet.Values.ToList();
            for (int i = 0; i < properties.Count; i++)
            {
                var property = properties[i];

                result[property.Scope].Add(new ClassCodeGenerationProperty
                {
                    Key = Util.Type.IsKey(property.Type, out _),
                    Type = new TypeFactory(Context).Build(property.Type),
                    Name = property.Name,
                    Initializer = new InitValueFactory(Context).Build(property.Type, property.Name)
                });
            }

            foreach (var (scope, p) in result)
            {
                var super = scope == Scope.Common;
                var inherit = !super && result[Scope.Common].Count > 0;
                if (!inherit && p.Count == 0)
                    continue;

                var code = _template.Render(new { Scope = scope, Name = tableName, Properties = p, Super = super, Inherit = inherit });
                yield return (scope, tableName, code);
                //var path = Path.Combine(_dir, $"{scope}".ToLower(), $"{tableName}.h");
                //File.WriteAllText(path, code);
            }
        }

        protected override void OnWorked(string input, (Scope Scope, string Name, string Code) output, int percent)
        {
            Logger.Write($"클래스 코드 파일을 저장했습니다. - {input}", percent: percent);
        }

        protected override IReadOnlyList<(Scope Scope, string Name, string Code)> OnFinish(IReadOnlyList<(Scope Scope, string Name, string Code)> output)
        {
            var template = Template.Parse(File.ReadAllText("Template/C++/class.complete.txt"));
            foreach (var g in output.GroupBy(x => x.Scope))
            {
                var scope = g.Key;
                var super = scope == Scope.Common;
                var code = template.Render(new { Scope = scope, Super = super, Codes = g.OrderBy(x => x.Name).Select(x => x.Code).ToList() });
                var path = Path.Combine(_dir, $"{scope}".ToLower(), $"model.h");
                File.WriteAllText(path, code);
            }

            Logger.Complete($"클래스 코드 파일을 저장했습니다.");
            return base.OnFinish(output);
        }
    }
}
