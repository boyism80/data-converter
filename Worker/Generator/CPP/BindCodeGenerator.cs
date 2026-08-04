using ExcelTableConverter.Configuration;
using ExcelTableConverter.Factory.CPP;
using ExcelTableConverter.Model;
using ExcelTableConverter.Util;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.CPP
{
    public class BindCodeGenerator : ParallelWorker<uint, KeyValuePair<uint, string>>
    {
        private static readonly Template _template = Template.Parse(File.ReadAllText($"Template/C++/container.txt"));

        public Dictionary<uint, string> Result { get; private set; } = new Dictionary<uint, string>();

        public BindCodeGenerator(Context ctx) : base(ctx)
        {
        }

        protected override IEnumerable<uint> OnReady()
        {
            foreach (var (scope, _) in Context.Configuration.DefinedScopes)
            {
                yield return scope;
            }
        }

        protected override IEnumerable<KeyValuePair<uint, string>> OnWork(uint scope)
        {
            var buffer = new List<object>();
            foreach (var (tableName, schemaSet) in Context.Completed.Schema.OrderBy(x => x.Key))
            {
                var ftdSchemaSet = schemaSet.Values.Where(x => AppConfiguration.ContainsScope(x.Scope, scope)).ToList();
                if (ftdSchemaSet.Count == 0)
                    continue;

                var ns = Util.CPP.Namespace.Access(Context.Configuration.Namespace);
                var modelName = $"{ns}{tableName}";

                var containerType = string.Empty;
                var genericType = string.Empty;
                var pk = ftdSchemaSet.FirstOrDefault(x => Util.Type.IsPrimaryKey(x.Type, out _));
                var gk = ftdSchemaSet.FirstOrDefault(x => Util.Type.IsGroupKey(x.Type, out _));
                if (gk != null && pk != null)
                {
                    containerType = $"{ns}kv_container";
                    genericType = $"{new TypeFactory(Context).Build(gk.Type)}, {ns}kv_container<{new TypeFactory(Context).Build(pk.Type)}, {modelName}>";
                }
                else if (pk != null)
                {
                    containerType = $"{ns}kv_container";
                    genericType = $"{new TypeFactory(Context).Build(pk.Type)}, {modelName}";
                }
                else if (gk != null)
                {
                    containerType = $"{ns}kv_container";
                    genericType = $"{new TypeFactory(Context).Build(gk.Type)}, {ns}array_container<{modelName}>";
                }
                else
                {
                    containerType = $"{ns}array_container";
                    genericType = modelName;
                }

                buffer.Add(new
                {
                    Name = tableName,
                    Type = containerType,
                    Generic = genericType,
                    Json = Context.Completed.Schema[tableName].Json,
                });
            }
            var obj = new ScribanEx
            {
                ["tables"] = buffer,
                ["config"] = Context.Configuration
            };
            var ctx = ScribanEx.CreateContext();
            ctx.PushGlobal(obj);
            var code = _template.Render(ctx);
            yield return new KeyValuePair<uint, string>(scope, code);
        }

        protected override void OnWorked(uint input, KeyValuePair<uint, string> output, int percent)
        {
            Result.Add(output.Key, output.Value);
            base.OnWorked(input, output, percent);
        }

        protected override IReadOnlyList<KeyValuePair<uint, string>> OnFinish(IReadOnlyList<KeyValuePair<uint, string>> output)
        {
            return base.OnFinish(output);
        }
    }
}
