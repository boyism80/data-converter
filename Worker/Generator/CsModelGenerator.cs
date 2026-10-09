using ExcelTableConverter.Factory.CS;
using ExcelTableConverter.Model;
using Scriban.Runtime;

namespace ExcelTableConverter.Worker.Generator
{
    public class CsModelGenerator : ModelGenerator
    {
        private static readonly HashSet<string> _constantTypes = new() { "bool", "byte", "sbyte", "char", "decimal", "double", "float", "int", "uint", "nint", "nuint", "long", "ulong", "short", "ushort", "string" };
        private readonly TypeFactory _types;

        public CsModelGenerator(Context ctx) : base(ctx)
        {
            _types = new TypeFactory(ctx);
        }

        protected override Language Language => Language.CSharp;
        protected override string FileName => "Model.cs";

        protected override ScriptObject Globals()
        {
            return new ScriptObject
            {
                ["ns"] = new ScriptObject
                {
                    ["root"] = Language.Namespace(Context.RootNamespace),
                    ["enum"] = Language.Namespace(Context.EnumNamespace),
                    ["const"] = Language.Namespace(Context.ConstNamespace),
                    ["dsl_type"] = Language.Qualify(Context.EnumNamespace, Config.DslTypeEnumName),
                },
            };
        }

        protected override object Property(SchemaData column, int index) => new
        {
            Name = column.Name,
            Identifier = Language.Identifier(column.Name),
            Type = _types.Build(column.Type),
        };

        protected override List<object> EnumMembers(string name, List<KeyValuePair<string, EnumExpression>> members)
        {
            return members.Select(x => new
            {
                Name = x.Key,
                Identifier = Language.Identifier(x.Key),
                Value = x.Value.Format(Language.Identifier),
            } as object).ToList();
        }

        protected override object Const(ConstData constData)
        {
            var type = _types.Build(constData.Type);
            return new
            {
                Name = constData.Name,
                Identifier = Language.Identifier(constData.Name),
                Modifier = _constantTypes.Contains(type) ? "const" : "static readonly",
                Type = type,
                Value = new AllocateValueFactory(Context).Build(constData.Type, constData.Value),
            };
        }

        protected override object DslParameter(DSLParameter parameter, int index)
        {
            var serialize = string.Empty;
            if (ColumnType.Parse(Context.Completed.Schema.RootType(parameter.Type)).Naked == "int")
                serialize = ColumnType.Parse(parameter.Type).Nullable ? $"{parameter.Name} == null ? (long?)null : (long?)(long)" : "(long)";

            return new
            {
                Name = parameter.Name,
                Identifier = Language.Identifier(parameter.Name),
                Type = _types.Build(parameter.Type),
                Serialize = serialize,
                Deserialize = new ValueDeserializeFactory(Context).Build(parameter.Type, $"parameters[{index}]"),
            };
        }

        protected override object Container(string table, SchemaData pk, SchemaData gk)
        {
            var model = Language.Identifier(table);
            var (type, generic) = (pk, gk) switch
            {
                (not null, not null) => ("KeyValueContainer", $"{_types.Build(gk.Type)}, KeyValueContainer<{_types.Build(pk.Type)}, {model}>"),
                (not null, null) => ("KeyValueContainer", $"{_types.Build(pk.Type)}, {model}"),
                (null, not null) => ("KeyValueContainer", $"{_types.Build(gk.Type)}, ArrayContainer<{model}>"),
                _ => ("ArrayContainer", model),
            };
            return new { Name = table, Identifier = model, Type = type, Generic = generic };
        }
    }
}
