using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory.CPP
{
    public class JsonAppendValueFactory : ExpressionFormatFactory<string>
    {
        public JsonAppendValueFactory(Context ctx) : base(ctx)
        { }

        private static string Scalar(DataType type, string name)
        {
            return type.Nullable ? $"{name}.has_value() ? {name}.value() : Json::nullValue" : name;
        }

        protected override string ByteType(DataType type, string name) => Scalar(type, name);
        protected override string SbyteType(DataType type, string name) => Scalar(type, name);
        protected override string ShortType(DataType type, string name) => Scalar(type, name);
        protected override string UshortType(DataType type, string name) => Scalar(type, name);
        protected override string BoolType(DataType type, string name) => Scalar(type, name);
        protected override string IntType(DataType type, string name) => Scalar(type, name);
        protected override string UintType(DataType type, string name) => Scalar(type, name);
        protected override string LongType(DataType type, string name) => Scalar(type, name);
        protected override string UlongType(DataType type, string name) => Scalar(type, name);
        protected override string DoubleType(DataType type, string name) => Scalar(type, name);
        protected override string FloatType(DataType type, string name) => Scalar(type, name);
        protected override string TimeSpanType(DataType type, string name) => Scalar(type, name);
        protected override string DateTimeType(DataType type, string name) => Scalar(type, name);
        protected override string DateRangeType(DataType type, string name) => Scalar(type, name);
        protected override string PointType(GeometryDataType type, string name) => Scalar(type, name);
        protected override string SizeType(GeometryDataType type, string name) => Scalar(type, name);
        protected override string RangeType(GeometryDataType type, string name) => Scalar(type, name);
        protected override string AreaType(GeometryDataType type, string name) => Scalar(type, name);
        protected override string ArrayType(ArrayDataType type, string name) => name;
        protected override string MapType(MapDataType type, string name) => name;

        protected override string StringType(DataType type, string name)
        {
            return type.Nullable ? $"{name}.has_value() ? Json::Value({name}.value()) : Json::nullValue" : name;
        }

        protected override string DslType(DataType type, string name)
        {
            return type.Nullable ? $"{name}.has_value() ? {name}.value().to_json() : Json::nullValue" : $"{name}.to_json()";
        }

        protected override string EnumType(EnumDataType type, string name)
        {
            var ns = $"{Language.Cpp.Namespace(Context.EnumNamespace)}::";
            return type.Nullable ? $"{name}.has_value() ? {ns}enum_tostring<{ns}{type.Naked}>({name}.value()) : Json::nullValue" : $"{ns}enum_tostring<{ns}{type.Naked}>({name})";
        }
    }
}
