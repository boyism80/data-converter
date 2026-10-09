using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory.CS
{
    public class TypeFactory : TypeFormatFactory<string>
    {
        public TypeFactory(Context ctx) : base(ctx)
        { }

        private static string Nullable(DataType type, string name)
        {
            return type.Nullable ? $"{name}?" : name;
        }

        protected override string ByteType(DataType type) => Nullable(type, "byte");
        protected override string SbyteType(DataType type) => Nullable(type, "sbyte");
        protected override string ShortType(DataType type) => Nullable(type, "short");
        protected override string UshortType(DataType type) => Nullable(type, "ushort");
        protected override string BoolType(DataType type) => type.Root;
        protected override string IntType(DataType type) => Nullable(type, "int");
        protected override string UintType(DataType type) => Nullable(type, "uint");
        protected override string LongType(DataType type) => Nullable(type, "long");
        protected override string UlongType(DataType type) => Nullable(type, "ulong");
        protected override string DoubleType(DataType type) => Nullable(type, "double");
        protected override string FloatType(DataType type) => Nullable(type, "float");
        protected override string StringType(DataType type) => "string";
        protected override string DslType(DataType type) => Nullable(type, "Dsl");
        protected override string TimeSpanType(DataType type) => Nullable(type, "TimeSpan");
        protected override string DateTimeType(DataType type) => type.Root;
        protected override string DateRangeType(DataType type) => type.Root;
        protected override string ArrayType(ArrayDataType type) => $"List<{Build(type.Element)}>";
        protected override string MapType(MapDataType type) => $"Dictionary<{Build(type.Key)}, {Build(type.Value)}>";
        protected override string PointType(GeometryDataType type) => Nullable(type, $"Point<{Build(type.Element)}>");
        protected override string SizeType(GeometryDataType type) => Nullable(type, $"Size<{Build(type.Element)}>");
        protected override string RangeType(GeometryDataType type) => Nullable(type, $"Range<{Build(type.Element)}>");
        protected override string AreaType(GeometryDataType type) => Nullable(type, $"Area<{Build(type.Element)}>");

        protected override string EnumType(EnumDataType type)
        {
            return Nullable(type, Language.CSharp.Qualify(Context.EnumNamespace, type.Naked));
        }
    }
}
