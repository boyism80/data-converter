using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory.Node
{
    public class TypeBuilderFactory : TypeFormatFactory<string>
    {
        public TypeBuilderFactory(Context ctx) : base(ctx)
        { }

        protected override string ByteType(DataType type) => "DefaultBuilder().build";
        protected override string SbyteType(DataType type) => "DefaultBuilder().build";
        protected override string ShortType(DataType type) => "DefaultBuilder().build";
        protected override string UshortType(DataType type) => "DefaultBuilder().build";
        protected override string BoolType(DataType type) => "DefaultBuilder().build";
        protected override string IntType(DataType type) => "DefaultBuilder().build";
        protected override string UintType(DataType type) => "DefaultBuilder().build";
        protected override string LongType(DataType type) => "DefaultBuilder().build";
        protected override string UlongType(DataType type) => "DefaultBuilder().build";
        protected override string DoubleType(DataType type) => "DefaultBuilder().build";
        protected override string FloatType(DataType type) => "DefaultBuilder().build";
        protected override string StringType(DataType type) => "DefaultBuilder().build";
        protected override string DslType(DataType type) => "DslBuilder().build";
        protected override string TimeSpanType(DataType type) => "TimeSpanBuilder().build";
        protected override string DateTimeType(DataType type) => "DateTimeBuilder().build";
        protected override string DateRangeType(DataType type) => "DateRangeBuilder().build";
        protected override string ArrayType(ArrayDataType type) => $"ArrayBuilder({Build(type.Element)}).build";
        protected override string MapType(MapDataType type) => $"DictionaryBuilder({Build(type.Key)}, {Build(type.Value)}).build";
        protected override string PointType(GeometryDataType type) => "PointBuilder().build";
        protected override string SizeType(GeometryDataType type) => "SizeBuilder().build";
        protected override string RangeType(GeometryDataType type) => "RangeBuilder().build";
        protected override string AreaType(GeometryDataType type) => "AreaBuilder().build";
        protected override string EnumType(EnumDataType type) => $"EnumBuilder(\"{type.Naked}\").build";
    }
}
