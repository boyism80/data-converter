using ExcelTableConverter.Factory.CS;
using ExcelTableConverter.Model;
using ExcelTableConverter.Util;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.CS
{
    public class BindCodeGenerator : ParallelWorker<Scope, KeyValuePair<Scope, string>>
    {
        private static readonly Template _template = Template.Parse(File.ReadAllText($"Template/C#/container.txt"));

        public Dictionary<Scope, string> Result { get; private set; } = new Dictionary<Scope, string>();

        public BindCodeGenerator(Context ctx) : base(ctx)
        {
        }

        protected override IEnumerable<Scope> OnReady()
        {
            foreach (var scope in new[] { Scope.Server, Scope.Client })
            {
                yield return scope;
            }
        }

        protected override IEnumerable<KeyValuePair<Scope, string>> OnWork(Scope scope)
        {
            var buffer = new List<object>();
            foreach (var (tableName, schemaSet) in Context.Result.Schema.OrderBy(x => x.Key))
            {
                var ftdSchemaSet = schemaSet.Values.Where(x => x.Scope.HasFlag(scope)).ToList();
                if (ftdSchemaSet.Count == 0)
                    continue;

                var camelTableName = ScribanEx.UpperCamel(tableName);

                var containerType = string.Empty;
                var genericType = string.Empty;
                var pk = ftdSchemaSet.FirstOrDefault(x => Util.Type.IsPrimaryKey(x.Type, out _));
                var gk = ftdSchemaSet.FirstOrDefault(x => Util.Type.IsGroupKey(x.Type, out _));
                if (gk != null && pk != null)
                {
                    containerType = $"KeyValueContainer";
                    genericType = $"{new TypeFactory(Context).Build(gk.Type)}, KeyValueContainer<{new TypeFactory(Context).Build(pk.Type)}, {camelTableName}>";
                }
                else if (pk != null)
                {
                    containerType = $"KeyValueContainer";
                    genericType = $"{new TypeFactory(Context).Build(pk.Type)}, {camelTableName}";
                }
                else if (gk != null)
                {
                    containerType = $"KeyValueContainer";
                    genericType = $"{new TypeFactory(Context).Build(gk.Type)}, ArrayContainer<{camelTableName}>";
                }
                else
                {
                    containerType = $"ArrayContainer";
                    genericType = camelTableName;
                }

                buffer.Add(new
                {
                    Name = tableName,
                    Type = containerType,
                    Generic = genericType,
                    Json = Context.Result.Schema[tableName].Json,
                });
            }

            var obj = new ScribanEx();
            obj.Add("scope", scope);
            obj.Add("tables", buffer);
            obj.Add("config", Context.Config);

            var ctx = new TemplateContext();
            ctx.PushGlobal(obj);
            var code = _template.Render(ctx);
            yield return new KeyValuePair<Scope, string>(scope, code);
        }

        protected override void OnWorked(Scope input, KeyValuePair<Scope, string> output, int percent)
        {
            Result.Add(output.Key, output.Value);
            base.OnWorked(input, output, percent);
        }

        protected override IReadOnlyList<KeyValuePair<Scope, string>> OnFinish(IReadOnlyList<KeyValuePair<Scope, string>> output)
        {
            return base.OnFinish(output);
        }
    }
}
