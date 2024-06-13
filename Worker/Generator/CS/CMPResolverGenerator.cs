using ExcelTableConverter.Factory.CS;
using ExcelTableConverter.Model;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.CS
{
    public class CMPResolverGenerator : ParallelWorker<bool, bool>
    {
        private static readonly Template _template = Template.Parse(File.ReadAllText($"Template/C#/custom_message_pack_resolver.txt"));
        private readonly string _dir;

        public CMPResolverGenerator(Context ctx) : base(ctx)
        {
            _dir = Path.Combine(ctx.Output, Context.Config.CMPResolverCodeFilePath);
        }
        protected override IEnumerable<bool> OnReady()
        {
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

            yield return true;
        }

        protected override IEnumerable<bool> OnWork(bool value)
        {
            var properties = new List<object>();
            var index = 0;
            foreach (var (tableName, schemaSet) in Context.Result.Schema.OrderBy(x => x.Key))
            {
                var ftdSchemaSet = schemaSet.Values.Where(x => x.Scope.HasFlag(Scope.Client)).ToList();
                if (ftdSchemaSet.Count == 0)
                    continue;

                var containerType = string.Empty;
                var genericType = string.Empty;
                var pk = ftdSchemaSet.FirstOrDefault(x => Util.Type.IsPrimaryKey(x.Type, out _));
                var gk = ftdSchemaSet.FirstOrDefault(x => Util.Type.IsGroupKey(x.Type, out _));
                if (gk != null && pk != null)
                {
                    containerType = "Dictionary";
                    genericType = $"{new TypeFactory(Context).Build(gk.Type)}, Dictionary<{new TypeFactory(Context).Build(pk.Type)}, {tableName}>";
                }
                else if (pk != null)
                {
                    containerType = "Dictionary";
                    genericType = $"{new TypeFactory(Context).Build(pk.Type)}, {tableName}";
                }
                else if (gk != null)
                {
                    containerType = "Dictionary";
                    genericType = $"{new TypeFactory(Context).Build(gk.Type)}, List<{tableName}>";
                }
                else
                {
                    containerType = "List";
                    genericType = tableName;
                }

                properties.Add(new
                {
                    Index = index++,
                    Name = tableName,
                    Type = containerType,
                    Generic = genericType
                });
            }
            var code = _template.Render(new { Namespaces = Context.Config.Namespace, Properties = properties });
            File.WriteAllText(Path.Combine(_dir, "CustomMessagePackResolver.cs"), code);
            yield return true;
        }

        protected override void OnWorked(bool input, bool output, int percent)
        {
            Logger.Write("커스텀 메시지팩 코드를 생성했습니다.", percent: percent);
        }

        protected override IReadOnlyList<bool> OnFinish(IReadOnlyList<bool> output)
        {
            Logger.Complete("커스텀 메시지팩 코드를 생성했습니다.");
            return base.OnFinish(output);
        }
    }
}
