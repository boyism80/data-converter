using ExcelTableConverter.Configuration;
using ExcelTableConverter.Factory.CPP;
using ExcelTableConverter.Model;
using ExcelTableConverter.Util;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.CPP
{
    public class ClassFileGeneratorResult
    {
        public uint Scope { get; set; }
        public string Name { get; set; }
        public List<object> Props { get; set; }
    }

    public class ClassFileGenerator : ParallelWorker<string, ClassFileGeneratorResult>
    {
        private readonly string _dir;

        public ClassFileGenerator(Context ctx) : base(ctx)
        {
            _dir = Path.Join(Context.Output, "C++");
            foreach (var (_, scopeName) in Context.Configuration.DefinedScopes)
            {
                var path = Path.Join(_dir, scopeName);
                if (Directory.Exists(path) == false)
                    Directory.CreateDirectory(path);
            }
        }

        private string GenerateClassCode(List<object> items)
        {
            var obj = new ScribanEx
            {
                ["items"] = items,
                ["config"] = Context.Configuration,
            };

            var ctx = ScribanEx.CreateContext();
            ctx.PushGlobal(obj);

            var template = Template.Parse(File.ReadAllText("Template/C++/class.txt"));
            return template.Render(ctx);
        }

        private string GenerateTypeCode()
        {
            var obj = new ScribanEx
            {
                ["config"] = Context.Configuration,
            };
            var ctx = ScribanEx.CreateContext();
            ctx.PushGlobal(obj);

            var template = Template.Parse(File.ReadAllText("Template/C++/type.txt"));
            return template.Render(ctx);
        }

        private string GenerateDateTimeCode()
        {
            var obj = new ScribanEx
            {
                ["config"] = Context.Configuration
            };

            var ctx = ScribanEx.CreateContext();
            ctx.PushGlobal(obj);

            var template = Template.Parse(File.ReadAllText("Template/C++/datetime.txt"));
            return template.Render(ctx);
        }

        private string GenerateLuaCode(EnumCodeGenerator enumCodeGenerator)
        {
            var obj = new ScribanEx
            {
                ["enums"] = enumCodeGenerator.Enums,
                ["config"] = Context.Configuration,
            };

            var ctx = ScribanEx.CreateContext();
            ctx.PushGlobal(obj);

            var template = Template.Parse(File.ReadAllText("Template/C++/lua.txt"));
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

                var ccgp = new
                {
                    Key = Util.Type.IsKey(property.Type, out _),
                    Type = new TypeFactory(Context).Build(property.Type),
                    Name = property.Name,
                    Initializer = new InitValueFactory(Context).Build(property.Type, property.Name)
                };

                foreach (var (scope, _) in Context.Configuration.DefinedScopes)
                {
                    if (AppConfiguration.ContainsScope(property.Scope, scope))
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
            Logger.Write($"클래스 코드 파일을 저장했습니다. - {input}");
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
                return x.OrderBy(x => Context.Completed.Schema.GetInheritanceLevel(x.Name)).ThenBy(x => x.Name).Select(x => new
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

            var ctx = ScribanEx.CreateContext();
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
                    ["lua"] = GenerateLuaCode(enumCodeGenerator) + "\n" + constCodeGenerator.LuaCode,
                    ["config"] = Context.Configuration,
                };
                ctx.PushGlobal(obj);
                File.WriteAllText(Path.Combine(_dir, Context.Configuration.GetScopeName(scope), $"model.h"), modelTemplate.Render(ctx));
                ctx.PopGlobal();
            }

            File.WriteAllText(Path.Combine(_dir, $"datetime.h"), GenerateDateTimeCode());

            Logger.Complete($"클래스 코드 파일을 저장했습니다.");
            return base.OnFinish(output);
        }
    }
}
