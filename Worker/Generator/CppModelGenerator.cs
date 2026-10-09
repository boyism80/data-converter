using ExcelTableConverter.Factory.CPP;
using ExcelTableConverter.Model;
using Scriban.Runtime;

namespace ExcelTableConverter.Worker.Generator
{
    // Kind selects how const_lua.txt pushes a const value onto the Lua stack.
    public record LuaType(string Kind, string NakedType, LuaType Inner = null, LuaType Element = null, LuaType Key = null, LuaType Value = null, string EnumName = null, string EnumValue = null);

    public class CppModelGenerator : ModelGenerator
    {
        private static readonly HashSet<string> _constantTypes = new() { "bool", "char", "short", "int", "long", "long long", "float", "double", "int8_t", "int16_t", "int32_t", "int64_t", "uint8_t", "uint16_t", "uint32_t", "uint64_t", "char*" };
        private static readonly HashSet<string> _primitiveTypes = new() { "bool", "char", "short", "int", "long", "long long", "float", "double", "int8_t", "int16_t", "int32_t", "int64_t", "uint8_t", "uint16_t", "uint32_t", "uint64_t" };
        private readonly TypeFactory _types;
        private readonly AllocateValueFactory _values;
        private readonly ScriptObject _ns;

        public CppModelGenerator(Context ctx) : base(ctx)
        {
            _types = new TypeFactory(ctx);
            _values = new AllocateValueFactory(ctx);

            var root = Language.Namespace(ctx.RootNamespace);
            _ns = new ScriptObject
            {
                ["root"] = root,
                ["access"] = $"{root}::",
                ["enum"] = Language.LocalNamespace(ctx.EnumNamespace),
                ["enum_root"] = Language.Namespace(ctx.EnumNamespace),
                ["const"] = Language.LocalNamespace(ctx.ConstNamespace),
                ["const_root"] = Language.Namespace(ctx.ConstNamespace),
                ["dsl_type"] = Language.Qualify(ctx.EnumNamespace, Config.DslTypeEnumName),
            };
        }

        protected override Language Language => Language.Cpp;
        protected override string FileName => "model.h";

        public override void Run()
        {
            base.Run();

            var model = new ScriptObject { ["config"] = Config, ["ns"] = _ns };
            File.WriteAllText(Path.Join(Context.Output, Language.Name, "datetime.h"), Render("C++/datetime.txt", model));
        }

        protected override ScriptObject Globals()
        {
            var luaConsts = Context.Completed.Const
                .OrderBy(x => x.Key)
                .Where(x => x.Value.Count > 0)
                .Select(x => new GroupModel(x.Key, Model.Language.Lua.Constant(x.Key), x.Value.Values.Select(c => new { Name = c.Name, LuaName = Model.Language.Lua.Constant(c.Name), Type = LuaType(Context.Type(c.Type), c.Value) } as object).ToList()))
                .ToList();

            return new ScriptObject
            {
                ["ns"] = _ns,
                ["lua_consts"] = luaConsts,
            };
        }

        protected override object Property(SchemaData column, int index) => new
        {
            Name = column.Name,
            Type = _types.Build(column.Type),
            Initializer = new InitValueFactory(Context).Build(column.Type, column.Name),
        };

        protected override List<object> EnumMembers(string name, List<KeyValuePair<string, EnumExpression>> members)
        {
            return members.Select(x => new
            {
                Name = x.Key,
                Value = x.Value.Format(v => v),
            } as object).ToList();
        }

        protected override object Const(ConstData constData)
        {
            var type = _types.Build(constData.Type);
            if (type == "std::string")
                type = "char*";

            return new
            {
                Name = constData.Name,
                Declaration = _constantTypes.Contains(type) ? $"constexpr const {type}" : $"const {type}&",
                Value = _values.Build(constData.Type, constData.Value),
            };
        }

        protected override object DslParameter(DSLParameter parameter, int index)
        {
            var type = _types.Build(parameter.Type);
            var enumAccess = $"{Language.LocalNamespace(Context.EnumNamespace)}::";
            var argument = _primitiveTypes.Contains(type) || type.Contains(enumAccess) ? type : $"const {type}&";
            return new
            {
                Name = parameter.Name,
                Type = type,
                Argument = argument,
                Deserialize = $"any_cast<{argument}>(parameters[{index}])",
                AppendValue = new JsonAppendValueFactory(Context).Build(parameter.Type, parameter.Name),
            };
        }

        protected override object Container(string table, SchemaData pk, SchemaData gk)
        {
            var ns = $"{Language.Namespace(Context.RootNamespace)}::";
            var model = $"{ns}{table}";
            var (type, generic) = (pk, gk) switch
            {
                (not null, not null) => ($"{ns}kv_container", $"{_types.Build(gk.Type)}, {ns}kv_container<{_types.Build(pk.Type)}, {model}>"),
                (not null, null) => ($"{ns}kv_container", $"{_types.Build(pk.Type)}, {model}"),
                (null, not null) => ($"{ns}kv_container", $"{_types.Build(gk.Type)}, {ns}array_container<{model}>"),
                _ => ($"{ns}array_container", model),
            };
            return new { Name = table, Type = type, Generic = generic };
        }

        private LuaType LuaType(DataType dataType, DataValue value)
        {
            var naked = dataType.Naked;
            if (dataType.Nullable)
            {
                if (value is NullValue)
                    return new LuaType("null", naked);
                else
                    return new LuaType("nullable", naked, Inner: LuaType(Context.Type(naked), value));
            }

            switch (dataType)
            {
                case ArrayDataType array:
                    return new LuaType("array", naked, Element: LuaType(array.Element, NullValue.Instance));

                case MapDataType map:
                    return new LuaType("map", naked, Key: LuaType(map.Key, NullValue.Instance), Value: LuaType(map.Value, NullValue.Instance));

                case EnumDataType:
                    return new LuaType("enum", naked, EnumName: naked, EnumValue: (value as EnumValue)?.Name);

                case { Kind: DataKind.Bool }:
                    return new LuaType("bool", naked);

                case { Kind: DataKind.String }:
                    return new LuaType("string", naked);

                case { Kind: DataKind.Byte or DataKind.Sbyte or DataKind.Short or DataKind.Ushort or DataKind.Int or DataKind.Uint or DataKind.Long or DataKind.Ulong or DataKind.Float or DataKind.Double }:
                    return new LuaType("numeric", naked);

                case { Kind: DataKind.DateTime }:
                    return new LuaType("date_time", naked);

                case { Kind: DataKind.TimeSpan }:
                    return new LuaType("time_span", naked);

                case { Kind: DataKind.DateRange }:
                    return new LuaType("date_range", naked);

                case { Kind: DataKind.Point }:
                    return new LuaType("point", naked);

                case { Kind: DataKind.Range }:
                    return new LuaType("range", naked);

                case { Kind: DataKind.Size }:
                    return new LuaType("size", naked);

                case { Kind: DataKind.Area }:
                    return new LuaType("area", naked);

                default:
                    return new LuaType("unknown", naked);
            }
        }
    }
}
