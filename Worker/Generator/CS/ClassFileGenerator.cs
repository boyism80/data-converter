using ExcelTableConverter.Factory.CS;
using ExcelTableConverter.Model;
using ExcelTableConverter.Util;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.CS
{
    public class ClassFileGeneratorResult
    { 
        public Scope Scope { get; set; }
        public string Name { get; set; }
        public string Table { get; set; }
        public List<object> Props { get; set; }
    };

    public class ClassFileGenerator : ParallelWorker<string, ClassFileGeneratorResult>
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

            foreach (var scope in new[] { Scope.Server, Scope.Client })
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

        protected override IEnumerable<ClassFileGeneratorResult> OnWork(string tableName)
        {
            var schemaSet = Context.Result.Schema[tableName];
            var result = new[] { Scope.Server, Scope.Client }.ToDictionary(x => x, x => new List<object>());
            var properties = schemaSet.Values.ToList();
            for (int i = 0; i < properties.Count; i++)
            {
                var property = properties[i];
                if (property.Inherited)
                    continue;

                result[property.Scope].Add(new
                {
                    Index = i,
                    Key = Util.Type.IsKey(property.Type, out _),
                    Type = new TypeFactory(Context).Build(property.Type),
                    Name = property.Name.ToCamelCase()
                });
            }

            foreach (var (scope, props) in result)
            {
                if (props.Count == 0 && schemaSet.Based == null)
                    continue;

                yield return new ClassFileGeneratorResult
                {
                    Scope = scope,
                    Name = tableName.ToCamelCase(),
                    Table = tableName,
                    Props = props
                };
            }
        }

        protected override void OnWorked(string input, ClassFileGeneratorResult output, int percent)
        {
            Logger.Write($"클래스 코드 파일을 저장했습니다. - {input}", percent: percent);
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

            var g = output.GroupBy(x => x.Scope).ToDictionary(x => x.Key, x =>
            {
                return x.OrderBy(x => Context.GetInheritanceLevel(x.Table)).ThenBy(x => x.Table).Select(x => new
                {
                    x.Name,
                    x.Props,
                    Based = Context.Result.Schema[x.Table].Based.ToCamelCase()
                } as object).ToList();
            });

            if (g.ContainsKey(Scope.Server) == false)
                g.Add(Scope.Server, new List<object>());

            if (g.ContainsKey(Scope.Client) == false)
                g.Add(Scope.Client, new List<object>());

            var classTemplate = Template.Parse(File.ReadAllText("Template/C#/class.txt"));
            var modelTemplate = Template.Parse(File.ReadAllText("Template/C#/model.txt"));
            foreach (var (scope, items) in g)
            {
                var classCode = classTemplate.Render(new 
                { 
                    Namespace = Context.Config.Namespace.ConvertAll(x => x.ToCamelCase()), 
                    Scope = scope,
                    Items = items
                });

                File.WriteAllText(Path.Combine(_dir, $"{scope.ToString().ToLower()}", "Model.cs"), modelTemplate.Render(new
                {
                    Namespace = Context.Config.Namespace.ConvertAll(x => x.ToCamelCase()),
                    EnumNamespace = Context.Config.EnumNamespace.ConvertAll(x => x.ToCamelCase()),
                    ConstNamespace = Context.Config.ConstNamespace.ConvertAll(x => x.ToCamelCase()),
                    Scope = scope,
                    Class = classCode,
                    Enum = enumCodeGenerator.Result,
                    Const = constCodeGenerator.Result[scope],
                    Container = bindCodeGenerator.Result[scope],
                    Dsl = dslCodeGenerator.Result
                }));
            }

            Logger.Complete($"클래스 코드 파일을 저장했습니다.");
            return base.OnFinish(output);
        }
    }
}
