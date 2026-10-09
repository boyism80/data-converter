using ExcelTableConverter.Factory.Go;
using ExcelTableConverter.Model;
using Scriban.Runtime;
using System.Globalization;

namespace ExcelTableConverter.Worker.Generator
{
    public class GoModelGenerator : ModelGenerator
    {
        private readonly TypeFactory _types;
        private readonly TypeBuilderFactory _builders;

        public GoModelGenerator(Context ctx) : base(ctx)
        {
            _types = new TypeFactory(ctx);
            _builders = new TypeBuilderFactory(ctx);
        }

        protected override Language Language => Language.Go;
        protected override string FileName => "model.go";

        protected override ScriptObject Globals() => new ScriptObject();

        protected override object Property(SchemaData column, int index) => new
        {
            Name = column.Name,
            Identifier = Language.Identifier(column.Name),
            Type = _types.Build(column.Type),
            Initializer = $"New{_builders.Build(column.Type)}.Build(raw.{Language.Identifier(column.Name)})",
        };

        protected override List<object> EnumMembers(string name, List<KeyValuePair<string, EnumExpression>> members)
        {
            var prefix = $"{name}_";
            var values = new HashSet<string>();
            return members.Select(x =>
            {
                var member = Language.Constant($"{name}_{x.Key}");
                var value = x.Value.Format(s =>
                {
                    var hex = s.StartsWith("0x") ? s.Substring(2) : s;
                    if (int.TryParse(hex, NumberStyles.HexNumber, null, out _) || s.Trim() == "|")
                        return s;
                    else
                        return Language.Constant($"{name}_{s}");
                });

                return new
                {
                    Name = member,
                    Label = member.StartsWith(prefix, StringComparison.Ordinal) ? member.Substring(prefix.Length) : member,
                    Value = value,
                    Unique = values.Add(value),
                } as object;
            }).ToList();
        }

        protected override object Const(ConstData constData)
        {
            var type = _types.Build(constData.Type);
            var value = new AllocateValueFactory(Context).Build(constData.Type, constData.Value);
            return new
            {
                Name = constData.Name,
                Type = type,
                Value = type == "Duration" ? $"Duration({value})" : value,
            };
        }

        protected override object DslParameter(DSLParameter parameter, int index) => new
        {
            Name = parameter.Name,
            Identifier = Language.Identifier(parameter.Name),
            Type = _types.Build(parameter.Type),
            Deserialize = $"params[{index}].({_types.Build(parameter.Type)})",
        };

        protected override object Container(string table, SchemaData pk, SchemaData gk)
        {
            var model = Language.Identifier(table);
            var element = Context.Completed.Schema.BaseTables.Contains(table) ? $"{model}Interface" : model;
            var key = pk == null ? null : _types.Build(pk.Type);
            var group = gk == null ? null : _types.Build(gk.Type);
            var (kind, returnType) = (pk, gk) switch
            {
                (not null, not null) => ("group_primary", $"map[{group}]map[{key}]{element}"),
                (not null, null) => ("primary", $"map[{key}]{element}"),
                (null, not null) => ("group", $"map[{group}][]{element}"),
                _ => ("array", $"[]{element}"),
            };

            return new
            {
                Name = table,
                Identifier = model,
                IsAbstract = element != model,
                Kind = kind,
                KeyType = key,
                KeyBuilder = key == null ? null : $"New{_builders.Build(key)}",
                GroupType = group,
                GroupBuilder = group == null ? null : $"New{_builders.Build(group)}",
                ElementType = element,
                ReturnType = returnType,
            };
        }
    }
}
