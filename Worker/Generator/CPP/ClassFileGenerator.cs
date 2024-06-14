using ExcelTableConverter.Factory.CPP;
using ExcelTableConverter.Model;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.CPP
{
    public class ClassFileGenerator : ParallelWorker<string, (Scope Scope, string Name, List<object> Props)>
    {
        private readonly string _dir;

        public ClassFileGenerator(Context ctx) : base(ctx)
        {
            _dir = Path.Combine(ctx.Output, Context.Config.ClassCodeFilePath);
            if (Directory.Exists(_dir) == false)
                Directory.CreateDirectory(_dir);

            foreach (var dir in Directory.GetDirectories(_dir))
                Directory.Delete(dir, true);

            foreach (var file in Directory.GetFiles(_dir))
                File.Delete(file);

            foreach (var scope in new[] { Scope.Server, Scope.Client })
            {
                Directory.CreateDirectory(Path.Combine(_dir, $"{scope.ToString().ToLower()}"));
                Directory.CreateDirectory(Path.Combine(_dir, $"{scope.ToString().ToLower()}"));
            }
        }

        protected override IEnumerable<string> OnReady()
        {
            foreach (var tableName in Context.Result.Schema.Keys)
            {
                yield return tableName;
            }
        }

        protected override IEnumerable<(Scope, string, List<object>)> OnWork(string tableName)
        {
            var schemaSet = Context.Result.Schema[tableName];
            var result = new[] { Scope.Server, Scope.Client }.ToDictionary(x => x, x => new List<object>());
            var properties = schemaSet.Values.ToList();
            for (int i = 0; i < properties.Count; i++)
            {
                var property = properties[i];
                var ccgp = new
                {
                    Key = Util.Type.IsKey(property.Type, out _),
                    Type = new TypeFactory(Context).Build(property.Type),
                    Name = property.Name,
                    Initializer = new InitValueFactory(Context).Build(property.Type, property.Name)
                };

                foreach (var scope in new[] { Scope.Server, Scope.Client })
                {
                    if (property.Scope.HasFlag(scope))
                        result[scope].Add(ccgp);
                }
            }

            foreach (var (scope, props) in result)
            {
                if (props.Count == 0)
                    continue;

                yield return (scope, tableName, props);
            }
        }

        protected override void OnWorked(string input, (Scope Scope, string Name, List<object> Props) output, int percent)
        {
            Logger.Write($"클래스 코드 파일을 저장했습니다. - {input}", percent: percent);
        }

        protected override IReadOnlyList<(Scope Scope, string Name, List<object> Props)> OnFinish(IReadOnlyList<(Scope Scope, string Name, List<object> Props)> output)
        {
            var enumCodeGenerator = new EnumCodeGenerator(Context);
            enumCodeGenerator.Run();

            var dslCodeGenerator = new DslCodeGenerator(Context);
            dslCodeGenerator.Run();

            var constCodeGenerator = new ConstCodeGenerator(Context);
            constCodeGenerator.Run();

            var bindCodeGenerator = new BindCodeGenerator(Context);
            bindCodeGenerator.Run();

            var classTemplate = Template.Parse(File.ReadAllText("Template/C++/class.txt"));
            var baseTypeTemplate = Template.Parse(File.ReadAllText("Template/C++/type.txt"));
            var modelTemplate = Template.Parse(File.ReadAllText("Template/C++/model.txt"));
            foreach (var g in output.GroupBy(x => x.Scope))
            {
                var scope = g.Key;
                File.WriteAllText(Path.Combine(_dir, $"{scope.ToString().ToLower()}", $"model.h"), modelTemplate.Render(new
                {
                    Namespace = Util.CPP.Namespace.Access(Context.Config.Namespace),
                    Namespaces = Context.Config.Namespace,
                    Enum = enumCodeGenerator.Result,
                    Dsl = dslCodeGenerator.Result,
                    Type = baseTypeTemplate.Render(new { Namespace = Util.CPP.Namespace.Access(Context.Config.Namespace) }),
                    Const = constCodeGenerator.Result[scope],
                    Class = classTemplate.Render(new { Items = g.OrderBy(x => x.Name).Select(x => new { x.Name, x.Props }).ToList() }),
                    Container = bindCodeGenerator.Result[scope]
                }));
            }

            Logger.Complete($"클래스 코드 파일을 저장했습니다.");
            return base.OnFinish(output);
        }
    }
}
