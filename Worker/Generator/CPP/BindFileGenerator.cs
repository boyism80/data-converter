using ExcelTableConverter.Factory.CPP;
using ExcelTableConverter.Model;
using ExcelTableConverter.Model.CPP;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.CPP
{
    public class BindFileGenerator : ParallelWorker<Scope, bool>
    {
        private static readonly Dictionary<Scope, Template> _template = new Dictionary<Scope, Template>
        {
            [Scope.Server] = Template.Parse(File.ReadAllText($"Template/C++/bind.{Scope.Server.ToString().ToLower()}.txt")),
            [Scope.Client] = Template.Parse(File.ReadAllText($"Template/C++/bind.{Scope.Server.ToString().ToLower()}.txt")),
        };
        private readonly string _dir;

        public BindFileGenerator(Context ctx) : base(ctx)
        {
            _dir = Path.Combine(Context.Output, Context.Config.BindingCodeFilePath);
        }

        protected override IEnumerable<Scope> OnReady()
        {
            foreach (var scope in new[] { Scope.Server, Scope.Client })
            {
                var dir = Path.Combine(_dir, $"{scope}".ToLower());
                if (Directory.Exists(dir) == false)
                    Directory.CreateDirectory(dir);

                foreach (var file in Directory.GetFiles(dir))
                    File.Delete(file);

                yield return scope;
            }
        }

        protected override IEnumerable<bool> OnWork(Scope scope)
        {
            var template = _template[scope];
            var buffer = new List<BindingCodeGeneratorProperty>();
            foreach (var (tableName, schemaSet) in Context.Result.Schema.OrderBy(x => x.Key))
            {
                var ftdSchemaSet = schemaSet.Values.Where(x => x.Scope.HasFlag(scope)).ToList();
                if (ftdSchemaSet.Count == 0)
                    continue;

                var containerType = string.Empty;
                var genericType = string.Empty;
                var pk = ftdSchemaSet.FirstOrDefault(x => Util.Type.IsPrimaryKey(x.Type, out _));
                var gk = ftdSchemaSet.FirstOrDefault(x => Util.Type.IsGroupKey(x.Type, out _));
                if (gk != null && pk != null)
                {
                    containerType = "kv_container";
                    genericType = $"{new TypeFactory(Context).Build(gk.Type)}, kv_container<{new TypeFactory(Context).Build(pk.Type)}, {tableName}>";
                }
                else if (pk != null)
                {
                    containerType = "kv_container";
                    genericType = $"{new TypeFactory(Context).Build(pk.Type)}, {tableName}";
                }
                else if (gk != null)
                {
                    containerType = "kv_container";
                    genericType = $"{new TypeFactory(Context).Build(gk.Type)}, array_container<{tableName}>";
                }
                else
                {
                    containerType = "array_container";
                    genericType = tableName;
                }

                buffer.Add(new BindingCodeGeneratorProperty
                {
                    Name = tableName,
                    Type = containerType,
                    Generic = genericType
                });
            }

            var code = template.Render(new { Scope = scope, Tables = buffer });
            var path = Path.Combine(_dir, $"{scope}".ToLower(), "container.h");
            File.WriteAllText(path, code);
            yield return true;
        }

        protected override void OnWorked(Scope input, bool output, int percent)
        {
            Logger.Write($"테이블 연결 코드 파일을 생성했습니다. - {input}", percent: percent);
            base.OnWorked(input, output, percent);
        }

        protected override IReadOnlyList<bool> OnFinish(IReadOnlyList<bool> output)
        {
            Logger.Complete("테이블 연결 코드 파일을 생성했습니다.");
            return base.OnFinish(output);
        }
    }
}
