using ExcelTableConverter.Factory.CPP;
using ExcelTableConverter.Model;
using ExcelTableConverter.Model.CPP;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.CPP
{
    public class ClassFileGenerator : ParallelWorker<string, (Scope Scope, string Name, string Header, string Source)>
    {
        private static readonly Template _headerTemplate = Template.Parse(File.ReadAllText("Template/C++/class.header.txt"));
        private static readonly Template _sourceTemplate = Template.Parse(File.ReadAllText("Template/C++/class.source.txt"));
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

            Directory.CreateDirectory(Path.Combine(_dir, "include"));

            foreach (var scope in new[] { Scope.Common, Scope.Server, Scope.Client })
            {
                Directory.CreateDirectory(Path.Combine(_dir, $"{scope.ToString().ToLower()}", "include"));
                Directory.CreateDirectory(Path.Combine(_dir, $"{scope.ToString().ToLower()}", "source"));
            }
        }

        protected override IEnumerable<string> OnReady()
        {
            foreach (var tableName in Context.Result.Schema.Keys)
            {
                yield return tableName;
            }
        }

        protected override IEnumerable<(Scope, string, string, string)> OnWork(string tableName)
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

                var header = _headerTemplate.Render(new { Scope = scope, Name = tableName, Properties = p, Super = super, Inherit = inherit });
                var source = _sourceTemplate.Render(new { Scope = scope, Name = tableName, Properties = p, Super = super, Inherit = inherit });
                yield return (scope, tableName, header, source);
            }
        }

        protected override void OnWorked(string input, (Scope Scope, string Name, string Header, string Source) output, int percent)
        {
            Logger.Write($"클래스 코드 파일을 저장했습니다. - {input}", percent: percent);
        }

        protected override IReadOnlyList<(Scope Scope, string Name, string Header, string Source)> OnFinish(IReadOnlyList<(Scope Scope, string Name, string Header, string Source)> output)
        {
            File.WriteAllText(Path.Combine(_dir, "include", $"model.h"), Template.Parse(File.ReadAllText("Template/C++/model.txt")).Render());
            File.WriteAllText(Path.Combine(_dir, "include", $"type.h"), Template.Parse(File.ReadAllText("Template/C++/type.txt")).Render());

            var template = Template.Parse(File.ReadAllText("Template/C++/class.txt"));
            foreach (var g in output.GroupBy(x => x.Scope))
            {
                var codes = g.OrderBy(x => x.Name).Select(x => (Header: x.Header, Source: x.Source)).ToList();
                var scope = g.Key;
                var super = scope == Scope.Common;
                var code = template.Render(new { Scope = scope, Super = super, Headers = codes.Select(x => x.Header).ToList(), Sources = codes.Select(x => x.Source).ToList() });
                var path = Path.Combine(_dir, $"{scope.ToString().ToLower()}", "include", "class.h");
                File.WriteAllText(path, code);
            }

            Logger.Complete($"클래스 코드 파일을 저장했습니다.");
            return base.OnFinish(output);
        }
    }
}
