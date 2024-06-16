using ExcelTableConverter.Factory.Node;
using ExcelTableConverter.Model;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.Node
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
                    Name = property.Name,
                    Initializer = new InitValueFactory(Context).Build(property.Type, property.Name)
                };

                foreach (var scope in new[] { Scope.Server, Scope.Client })
                {
                    if (property.Scope.HasFlag(scope))
                    {
                        result[scope].Add(ccgp);
                    }
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

            var classTemplate = Template.Parse(File.ReadAllText("Template/Node/class.txt"));
            var modelTemplate = Template.Parse(File.ReadAllText("Template/Node/model.txt"));

            var g = output.Count > 0 ?
                output.GroupBy(x => x.Scope).ToDictionary(x => x.Key, x =>
                {
                    return x.OrderBy(x => x.Name).Select(x => new
                    {
                        x.Name,
                        x.Props
                    } as object).ToList();
                }) :
                new Dictionary<Scope, List<object>>
                {
                    [Scope.Server] = new List<object>(),
                    [Scope.Client] = new List<object>()
                };

            foreach (var (scope, items) in g)
            {
                File.WriteAllText(Path.Combine(_dir, $"{scope.ToString().ToLower()}", $"model.js"), modelTemplate.Render(new
                {
                    Enum = enumCodeGenerator.Result,
                    Dsl = dslCodeGenerator.Result,
                    Const = constCodeGenerator.Result[scope],
                    Class = classTemplate.Render(new { Items = items }),
                    Bind = bindCodeGenerator.Result[scope],
                    Tables = Context.Result.Schema.Where(x =>
                    {
                        var schemaSet = x.Value;
                        var filter = schemaSet.Values.Where(x => x.Scope.HasFlag(scope)).ToList();
                        return filter.Count > 0;
                    }).Select(x => x.Key).OrderBy(x => x).ToList()
                }));
            }

            Logger.Complete($"클래스 코드 파일을 저장했습니다.");
            return base.OnFinish(output);
        }
    }
}
