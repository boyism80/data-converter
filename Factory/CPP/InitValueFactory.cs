using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory.CPP
{
    public class InitValueFactory : ExpressionFormatFactory<string>
    {
        private readonly TypeFactory _types;

        public InitValueFactory(Context ctx) : base(ctx)
        {
            _types = new TypeFactory(ctx);
        }

        private string Root => $"{Language.Cpp.Namespace(Context.RootNamespace)}::";

        private string Optional(DataType type, string name, string field)
        {
            return Plain(type.Nullable ? $"std::optional<{name}>" : name, field);
        }

        private string Plain(string name, string field)
        {
            return $"{Root}build<{name}>(json[\"{field}\"])";
        }

        protected override string ByteType(DataType type, string field) => Optional(type, "uint8_t", field);
        protected override string SbyteType(DataType type, string field) => Optional(type, "int8_t", field);
        protected override string ShortType(DataType type, string field) => Optional(type, "int16_t", field);
        protected override string UshortType(DataType type, string field) => Optional(type, "uint16_t", field);
        protected override string BoolType(DataType type, string field) => Optional(type, "bool", field);
        protected override string IntType(DataType type, string field) => Optional(type, "int", field);
        protected override string UintType(DataType type, string field) => Optional(type, "uint32_t", field);
        protected override string LongType(DataType type, string field) => Optional(type, "int64_t", field);
        protected override string UlongType(DataType type, string field) => Optional(type, "uint64_t", field);
        protected override string DoubleType(DataType type, string field) => Optional(type, "double", field);
        protected override string FloatType(DataType type, string field) => Optional(type, "float", field);
        protected override string StringType(DataType type, string field) => Optional(type, "std::string", field);
        protected override string DslType(DataType type, string field) => Optional(type, "dsl", field);
        protected override string TimeSpanType(DataType type, string field) => Optional(type, "timespan", field);
        protected override string DateTimeType(DataType type, string field) => Optional(type, "datetime", field);
        protected override string DateRangeType(DataType type, string field) => Optional(type, $"{Root}date_range", field);
        protected override string ArrayType(ArrayDataType type, string field) => Plain($"std::vector<{_types.Build(type.Element)}>", field);
        protected override string MapType(MapDataType type, string field) => Plain($"std::map<{_types.Build(type.Key)}, {_types.Build(type.Value)}>", field);
        protected override string PointType(GeometryDataType type, string field) => Optional(type, $"point<{_types.Build(type.Element)}>", field);
        protected override string SizeType(GeometryDataType type, string field) => Optional(type, $"size<{_types.Build(type.Element)}>", field);
        protected override string RangeType(GeometryDataType type, string field) => Optional(type, $"range<{_types.Build(type.Element)}>", field);
        protected override string AreaType(GeometryDataType type, string field) => Optional(type, $"area<{_types.Build(type.Element)}>", field);
        protected override string EnumType(EnumDataType type, string field) => Optional(type, Language.Cpp.Qualify(Context.EnumNamespace, type.Naked), field);
    }
}
