using ExcelTableConverter.Factory.CPP;
using ExcelTableConverter.Model;
using ExcelTableConverter.Util;
using NPOI.OpenXmlFormats.Spreadsheet;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.CPP
{
    public class ClassFileGeneratorResult
    {
        public Scope Scope { get; set; }
        public string Name { get; set; }
        public List<object> Props { get; set; }
    }

    public class ClassFileGenerator : ParallelWorker<string, ClassFileGeneratorResult>
    {
        private readonly string _dir;

        public ClassFileGenerator(Context ctx) : base(ctx)
        {
            _dir = Path.Join(Context.Output, "C++");
            foreach (var scope in new[] { Scope.Server, Scope.Client })
            {
                var path = Path.Join(_dir, $"{scope}".ToLower());
                if (Directory.Exists(path) == false)
                    Directory.CreateDirectory(path);
            }
        }

        private static string GenerateClassCode(List<object> items)
        {
            var obj = new ScribanEx
            {
                ["items"] = items,
                ["config"] = Context.Config,
            };

            var ctx = new TemplateContext();
            ctx.PushGlobal(obj);

            var template = Template.Parse(File.ReadAllText("Template/C++/class.txt"));
            return template.Render(ctx);
        }

        private static string GenerateTypeCode()
        {
            var obj = new ScribanEx
            {
                ["config"] = Context.Config,
            };
            var ctx = new TemplateContext();
            ctx.PushGlobal(obj);

            var template = Template.Parse(File.ReadAllText("Template/C++/type.txt"));
            return template.Render(ctx);
        }

        private static string GenerateDateTimeCode()
        {
            var obj = new ScribanEx
            {
                ["config"] = Context.Config
            };

            var ctx = new TemplateContext();
            ctx.PushGlobal(obj);

            var template = Template.Parse(File.ReadAllText("Template/C++/datetime.txt"));
            return template.Render(ctx);
        }

        private static string GenerateLuaCode(EnumCodeGenerator enumCodeGenerator)
        {
            var obj = new ScribanEx
            {
                ["enums"] = enumCodeGenerator.Enums,
                ["config"] = Context.Config,
            };

            var ctx = new TemplateContext();
            ctx.PushGlobal(obj);

            var template = Template.Parse(File.ReadAllText("Template/C++/lua.txt"));
            return template.Render(ctx);
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
                if (props.Count == 0 && schemaSet.Based == null)
                    continue;

                yield return new ClassFileGeneratorResult
                {
                    Scope = scope,
                    Name = tableName,
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

            var dslCodeGenerator = new DslCodeGenerator(Context);
            dslCodeGenerator.Run();

            var constCodeGenerator = new ConstCodeGenerator(Context);
            constCodeGenerator.Run();

            var bindCodeGenerator = new BindCodeGenerator(Context);
            bindCodeGenerator.Run();

            var modelTemplate = Template.Parse(File.ReadAllText("Template/C++/model.txt"));
            var g = output.GroupBy(x => x.Scope).ToDictionary(x => x.Key, x =>
            {
                return x.OrderBy(x => Context.GetInheritanceLevel(x.Name)).ThenBy(x => x.Name).Select(x => new
                {
                    x.Name,
                    x.Props,
                    Context.Result.Schema[x.Name].Based
                } as object).ToList();
            });

            if (g.ContainsKey(Scope.Server) == false)
                g.Add(Scope.Server, new List<object>());

            if (g.ContainsKey(Scope.Client) == false)
                g.Add(Scope.Client, new List<object>());

            var ctx = new TemplateContext();
            foreach (var (scope, items) in g)
            {
                var obj = new ScribanEx
                {
                    ["enum"] = enumCodeGenerator.Declaration,
                    ["type"] = GenerateTypeCode(),
                    ["const"] = constCodeGenerator.Declaration[scope],
                    ["class"] = GenerateClassCode(items),
                    ["dsl"] = dslCodeGenerator.Result,
                    ["container"] = bindCodeGenerator.Result[scope],
                    ["lua"] = GenerateLuaCode(enumCodeGenerator),
                    ["config"] = Context.Config,
                };
                ctx.PushGlobal(obj);
                File.WriteAllText(Path.Combine(_dir, $"{scope.ToString().ToLower()}", $"model.h"), modelTemplate.Render(ctx));
                ctx.PopGlobal();
            }

            File.WriteAllText(Path.Combine(_dir, $"datetime.h"), GenerateDateTimeCode());

            Logger.Complete($"클래스 코드 파일을 저장했습니다.");
            return base.OnFinish(output);
        }
    }
}
