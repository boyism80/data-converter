using ExcelTableConverter.Factory.Node;
using ExcelTableConverter.Model;
using Scriban.Runtime;
using System.Text.RegularExpressions;

namespace ExcelTableConverter.Worker.Generator
{
    public class NodeModelGenerator : ModelGenerator
    {
        private static readonly Regex _identifier = new Regex(@"^[_a-zA-Z][_a-zA-Z0-9]*", RegexOptions.Compiled);
        private readonly TypeBuilderFactory _builders;
        private readonly AllocateValueFactory _values;

        public NodeModelGenerator(Context ctx) : base(ctx)
        {
            _builders = new TypeBuilderFactory(ctx);
            _values = new AllocateValueFactory(ctx);
        }

        protected override Language Language => Language.Node;
        protected override string FileName => "model.js";

        protected override ScriptObject Globals() => new ScriptObject();

        protected override object Property(SchemaData column, int index) => new
        {
            Name = column.Name,
            Initializer = $"{_builders.Build(column.Type)}(v.{column.Name})",
        };

        protected override List<object> EnumMembers(string name, List<KeyValuePair<string, EnumExpression>> members)
        {
            return members.Select(x => new
            {
                Name = x.Key,
                Value = x.Value.Format(v => _identifier.IsMatch(v) ? $"this.{v}" : v),
            } as object).ToList();
        }

        protected override object Const(ConstData constData) => new
        {
            Name = constData.Name,
            Value = _values.Build(constData.Type, constData.Value),
        };

        protected override object DslParameter(DSLParameter parameter, int index) => new
        {
            Name = parameter.Name,
            Initializer = _builders.Build(parameter.Type),
        };

        protected override object Container(string table, SchemaData pk, SchemaData gk)
        {
            var model = $"{table}Builder().build";
            var type = (pk, gk) switch
            {
                (not null, not null) => $"DictionaryBuilder({_builders.Build(gk.Type)}, DictionaryBuilder({_builders.Build(pk.Type)}, {model}).build).build",
                (not null, null) => $"DictionaryBuilder({_builders.Build(pk.Type)}, {model}).build",
                (null, not null) => $"DictionaryBuilder({_builders.Build(gk.Type)}, ArrayBuilder({model}).build).build",
                _ => $"ArrayBuilder({model}).build",
            };
            return new { Name = table, Type = type };
        }
    }
}
