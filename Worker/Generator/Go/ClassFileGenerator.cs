using ExcelTableConverter.Configuration;
using ExcelTableConverter.Factory.Go;
using ExcelTableConverter.Model;
using ExcelTableConverter.Util;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.Go
{
    public class ClassFileGeneratorResult
    {
        public uint Scope { get; set; }
        public string Name { get; set; }
        public string Table { get; set; }
        public List<object> Props { get; set; }
        public bool IsAbstract { get; set; }
    };

    public class ClassFileGenerator : ParallelWorker<string, ClassFileGeneratorResult>
    {
        private readonly string _dir;
        private readonly HashSet<string> _baseTables = new HashSet<string>();

        public ClassFileGenerator(Context ctx) : base(ctx)
        {
            _dir = Path.Join(Context.Output, "Go");
            _baseTables = ctx.Completed.Schema.FindBaseTables();
            foreach (var (_, scopeName) in Context.Configuration.DefinedScopes)
            {
                var path = Path.Join(_dir, scopeName);
                if (Directory.Exists(path) == false)
                    Directory.CreateDirectory(path);
            }
        }

        private string GenerateClassCode(uint scope, List<object> items, HashSet<string> baseTables)
        {
            var obj = new ScribanEx
            {
                ["scope"] = scope,
                ["items"] = items,
                ["config"] = Context.Configuration,
                ["base_tables"] = baseTables,
            };

            var ctx = ScribanEx.CreateContext();
            ctx.PushGlobal(obj);

            var template = Template.Parse(File.ReadAllText("Template/Go/class.txt"));
            return template.Render(ctx);
        }

        protected override IEnumerable<string> OnReady()
        {
            foreach (var tableName in Context.Completed.Schema.Keys)
            {
                yield return tableName;
            }
        }

        protected override IEnumerable<ClassFileGeneratorResult> OnWork(string tableName)
        {
            var schemaSet = Context.Completed.Schema[tableName];
            var result = Context.Configuration.DefinedScopes.ToDictionary(x => x.Flag, x => new List<object>());
            var properties = schemaSet.Values.ToList();
            for (int i = 0; i < properties.Count; i++)
            {
                var property = properties[i];
                if (property.Inherited)
                    continue;

                var prop = new
                {
                    Index = i,
                    Key = Util.Type.IsKey(property.Type, out _),
                    Type = new TypeFactory(Context).Build(property.Type),
                    Name = property.Name,
                    Initializer = new InitValueFactory(Context).Build(property.Type, property.Name)
                };

                foreach (var (scope, _) in Context.Configuration.DefinedScopes)
                {
                    if (AppConfiguration.ContainsScope(property.Scope, scope))
                        result[scope].Add(prop);
                }
            }

            foreach (var (scope, props) in result)
            {
                if (props.Count == 0 && schemaSet.Based == null)
                    continue;

                yield return new ClassFileGeneratorResult
                {
                    Scope = scope,
                    Name = tableName,
                    Table = tableName,
                    Props = props,
                    IsAbstract = _baseTables.Contains(tableName)
                };
            }
        }

        protected override void OnWorked(string input, ClassFileGeneratorResult output, int percent)
        {
            Logger.Write($"클래스 코드 파일을 저장했습니다. - {input}");
        }

        protected override IReadOnlyList<ClassFileGeneratorResult> OnFinish(IReadOnlyList<ClassFileGeneratorResult> output)
        {
            var enumCodeGenerator = new EnumCodeGenerator(Context);
            enumCodeGenerator.Run();

            var constCodeGenerator = new ConstCodeGenerator(Context);
            constCodeGenerator.Run();

            var dslCodeGenerator = new DslCodeGenerator(Context);
            dslCodeGenerator.Run();

            var bindCodeGenerator = new BindCodeGenerator(Context);
            bindCodeGenerator.Run();

            // Find base tables that need interfaces
            var baseTables = Context.Completed.Schema.FindBaseTables();

            var g = output.GroupBy(x => x.Scope).ToDictionary(x => x.Key, x =>
            {
                return x.OrderBy(x => Context.Completed.Schema.GetInheritanceLevel(x.Table)).ThenBy(x => x.Table).Select(x => new
                {
                    x.Name,
                    x.Props,
                    Based = Context.Completed.Schema[x.Table].Based,
                    x.IsAbstract
                } as object).ToList();
            });

            foreach (var (scope, _) in Context.Configuration.DefinedScopes)
            {
                if (g.ContainsKey(scope) == false)
                    g.Add(scope, new List<object>());
            }

            var modelTemplate = Template.Parse(File.ReadAllText("Template/Go/model.txt"));
            foreach (var (scope, items) in g)
            {
                var obj = new ScribanEx
                {
                    ["scope"] = scope,
                    ["config"] = Context.Configuration,
                    ["class"] = GenerateClassCode(scope, items, baseTables),
                    ["enum"] = enumCodeGenerator.Result,
                    ["const"] = constCodeGenerator.Result[scope],
                    ["container"] = bindCodeGenerator.Result[scope],
                    ["dsl"] = dslCodeGenerator.Result,
                };
                var ctx = ScribanEx.CreateContext();
                ctx.PushGlobal(obj);

                File.WriteAllText(Path.Combine(_dir, Context.Configuration.GetScopeName(scope), "model.go"), modelTemplate.Render(ctx));
            }

            Logger.Complete($"클래스 코드 파일을 저장했습니다.");
            return base.OnFinish(output);
        }
    }
}
