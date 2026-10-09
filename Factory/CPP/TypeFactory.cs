using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory.CPP
{
    public class TypeFactory : TypeFormatFactory<string>
    {
        public TypeFactory(Context ctx) : base(ctx)
        { }

        private string Root => $"{Language.Cpp.Namespace(Context.RootNamespace)}::";

        private static string Optional(string type, bool nullable)
        {
            return nullable ? $"std::optional<{type}>" : type;
        }

        protected override string ByteType(DataType type) => Optional("uint8_t", type.Nullable);
        protected override string SbyteType(DataType type) => Optional("int8_t", type.Nullable);
        protected override string ShortType(DataType type) => Optional("int16_t", type.Nullable);
        protected override string UshortType(DataType type) => Optional("uint16_t", type.Nullable);
        protected override string BoolType(DataType type) => Optional(type.Naked, type.Nullable);
        protected override string IntType(DataType type) => Optional(type.Naked, type.Nullable);
        protected override string UintType(DataType type) => Optional("uint32_t", type.Nullable);
        protected override string LongType(DataType type) => Optional("int64_t", type.Nullable);
        protected override string UlongType(DataType type) => Optional("uint64_t", type.Nullable);
        protected override string DoubleType(DataType type) => Optional(type.Naked, type.Nullable);
        protected override string FloatType(DataType type) => Optional(type.Naked, type.Nullable);
        protected override string StringType(DataType type) => Optional("std::string", type.Nullable);
        protected override string DslType(DataType type) => Optional($"{Root}dsl", type.Nullable);
        protected override string TimeSpanType(DataType type) => Optional("timespan", type.Nullable);
        protected override string DateTimeType(DataType type) => Optional("datetime", type.Nullable);
        protected override string DateRangeType(DataType type) => Optional($"{Root}date_range", type.Nullable);
        protected override string ArrayType(ArrayDataType type) => $"std::vector<{Build(type.Element)}>";
        protected override string MapType(MapDataType type) => $"std::map<{Build(type.Key)}, {Build(type.Value)}>";
        protected override string PointType(GeometryDataType type) => Optional($"point<{Build(type.Element)}>", type.Nullable);
        protected override string SizeType(GeometryDataType type) => Optional($"size<{Build(type.Element)}>", type.Nullable);
        protected override string RangeType(GeometryDataType type) => Optional($"range<{Build(type.Element)}>", type.Nullable);
        protected override string AreaType(GeometryDataType type) => Optional($"area<{Build(type.Element)}>", type.Nullable);
        protected override string EnumType(EnumDataType type) => Optional(Language.Cpp.Qualify(Context.EnumNamespace, type.Naked), type.Nullable);
    }
}
