using ExcelTableConverter.Factory.Node;
using ExcelTableConverter.Model;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.Node
{
    public class BindCodeGenerator : ParallelWorker<Scope, KeyValuePair<Scope, string>>
    {
        private static readonly Template _template = Template.Parse(File.ReadAllText($"Template/Node/container.txt"));

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

                var containerType = string.Empty;
                var genericType = string.Empty;
                var pk = ftdSchemaSet.FirstOrDefault(x => Util.Type.IsPrimaryKey(x.Type, out _));
                var gk = ftdSchemaSet.FirstOrDefault(x => Util.Type.IsGroupKey(x.Type, out _));
                if (gk != null && pk != null)
                {
                    containerType = $"DictionaryBuilder({new TypeBuilderFactory(Context).Build(gk.Type)}, DictionaryBuilder({new TypeBuilderFactory(Context).Build(pk.Type)}, {tableName}Builder().build).build).build";
                }
                else if (pk != null)
                {
                    containerType = $"DictionaryBuilder({new TypeBuilderFactory(Context).Build(pk.Type)}, {tableName}Builder().build).build";
                }
                else if (gk != null)
                {
                    containerType = $"DictionaryBuilder({new TypeBuilderFactory(Context).Build(gk.Type)}, ArrayBuilder({tableName}Builder().build).build).build";
                }
                else
                {
                    containerType = $"ArrayBuilder({tableName}Builder().build).build";
                }

                buffer.Add(new
                {
                    Name = tableName,
                    Type = containerType,
                    Json = Context.Result.Schema[tableName].Json,
                });
            }

            var code = _template.Render(new { Scope = scope, Tables = buffer });
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
