using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory.Go
{
    public class TypeFactory : TypeFormatFactory<string>
    {
        public TypeFactory(Context ctx) : base(ctx)
        { }

        private static string Pointer(DataType type, string name)
        {
            return type.Nullable ? $"*{name}" : name;
        }

        protected override string ByteType(DataType type) => Pointer(type, "byte");
        protected override string SbyteType(DataType type) => Pointer(type, "int8");
        protected override string ShortType(DataType type) => Pointer(type, "int16");
        protected override string UshortType(DataType type) => Pointer(type, "uint16");
        protected override string BoolType(DataType type) => Pointer(type, "bool");
        protected override string IntType(DataType type) => Pointer(type, "int");
        protected override string UintType(DataType type) => Pointer(type, "uint32");
        protected override string LongType(DataType type) => Pointer(type, "int64");
        protected override string UlongType(DataType type) => Pointer(type, "uint64");
        protected override string DoubleType(DataType type) => Pointer(type, "float64");
        protected override string FloatType(DataType type) => Pointer(type, "float32");
        protected override string StringType(DataType type) => "string";
        protected override string DslType(DataType type) => Pointer(type, "Dsl");
        protected override string TimeSpanType(DataType type) => Pointer(type, "Duration");
        protected override string DateTimeType(DataType type) => Pointer(type, "time.Duration");
        protected override string DateRangeType(DataType type) => Pointer(type, "DateRange");
        protected override string ArrayType(ArrayDataType type) => $"[]{Build(type.Element)}";
        protected override string MapType(MapDataType type) => $"map[{Build(type.Key)}]{Build(type.Value)}";
        protected override string PointType(GeometryDataType type) => Pointer(type, $"Point[{Build(type.Element)}]");
        protected override string SizeType(GeometryDataType type) => Pointer(type, $"Size[{Build(type.Element)}]");
        protected override string RangeType(GeometryDataType type) => Pointer(type, $"Range[{Build(type.Element)}]");
        protected override string AreaType(GeometryDataType type) => Pointer(type, "Area");
        protected override string EnumType(EnumDataType type) => Pointer(type, type.Root);
    }
}
