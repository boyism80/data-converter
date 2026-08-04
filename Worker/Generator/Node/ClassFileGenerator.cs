using ExcelTableConverter.Configuration;
using ExcelTableConverter.Factory.Node;
using ExcelTableConverter.Model;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.Node
{
    public class ClassFileGenerator : ParallelWorker<string, (uint Scope, string Name, List<object> Props)>
    {
        private readonly string _dir;

        public ClassFileGenerator(Context ctx) : base(ctx)
        {
            _dir = Path.Join(Context.Output, "Node");
            foreach (var (_, scopeName) in Context.Configuration.DefinedScopes)
            {
                var path = Path.Join(_dir, scopeName);
                if (Directory.Exists(path) == false)
                    Directory.CreateDirectory(path);
            }
        }

        protected override IEnumerable<string> OnReady()
        {
            foreach (var tableName in Context.Completed.Schema.Keys)
            {
                yield return tableName;
            }
        }

        protected override IEnumerable<(uint Scope, string Name, List<object> Props)> OnWork(string tableName)
        {
            var schemaSet = Context.Completed.Schema[tableName];
            var result = Context.Configuration.DefinedScopes.ToDictionary(x => x.Flag, x => new List<object>());
            var properties = schemaSet.Values.ToList();
            for (int i = 0; i < properties.Count; i++)
            {
                var property = properties[i];
                if (property.Inherited)
                    continue;

                var ccgp = new
                {
                    Key = Util.Type.IsKey(property.Type, out _),
                    Name = property.Name,
                    Initializer = new InitValueFactory(Context).Build(property.Type, property.Name)
                };

                foreach (var (scope, _) in Context.Configuration.DefinedScopes)
                {
                    if (AppConfiguration.ContainsScope(property.Scope, scope))
                    {
                        result[scope].Add(ccgp);
                    }
                }
            }

            foreach (var (scope, props) in result)
            {
                if (props.Count == 0 && schemaSet.Based == null)
                    continue;

                yield return (scope, tableName, props);
            }
        }

        protected override void OnWorked(string input, (uint Scope, string Name, List<object> Props) output, int percent)
        {
            Logger.Write($"클래스 코드 파일을 저장했습니다. - {input}");
        }

        protected override IReadOnlyList<(uint Scope, string Name, List<object> Props)> OnFinish(IReadOnlyList<(uint Scope, string Name, List<object> Props)> output)
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

            var g = output.GroupBy(x => x.Scope).ToDictionary(x => x.Key, x =>
            {
                return x.OrderBy(x => Context.Completed.Schema.GetInheritanceLevel(x.Name)).Select(x => new
                {
                    x.Name,
                    x.Props,
                    Context.Completed.Schema[x.Name].Based
                } as object).ToList();
            });

            foreach (var (scope, _) in Context.Configuration.DefinedScopes)
            {
                if (g.ContainsKey(scope) == false)
                    g.Add(scope, new List<object>());
            }

            foreach (var (scope, items) in g)
            {
                File.WriteAllText(Path.Combine(_dir, Context.Configuration.GetScopeName(scope), $"model.js"), modelTemplate.Render(new
                {
                    Enum = enumCodeGenerator.Result,
                    Dsl = dslCodeGenerator.Result,
                    Const = constCodeGenerator.Result[scope],
                    Class = classTemplate.Render(new { Items = items }),
                    Bind = bindCodeGenerator.Result[scope],
                    Tables = Context.Completed.Schema.Where(x =>
                    {
                        var schemaSet = x.Value;
                        var filter = schemaSet.Values.Where(x => AppConfiguration.ContainsScope(x.Scope, scope)).ToList();
                        return filter.Count > 0;
                    }).Select(x => new { Name = x.Key, Json = Context.Completed.Schema[x.Key].Json }).OrderBy(x => x.Name).ToList()
                }));
            }

            Logger.Complete($"클래스 코드 파일을 저장했습니다.");
            return base.OnFinish(output);
        }
    }
}
