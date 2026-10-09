using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory.Go
{
    public class TypeBuilderFactory : TypeFormatFactory<string>
    {
        private readonly TypeFactory _types;

        public TypeBuilderFactory(Context ctx) : base(ctx)
        {
            _types = new TypeFactory(ctx);
        }

        private string Default(DataType type, string name)
        {
            var goType = _types.Build(name).TrimStart('*');
            return $"DefaultBuilder[{(type.Nullable ? $"*{goType}" : goType)}]()";
        }

        private string Func(DataType e, string trailing)
        {
            return $"func(rm json.RawMessage) ({_types.Build(e)}, error) {{{trailing}\r\n\t\treturn New{Build(e)}.Build(rm)\r\n    }}";
        }

        private string Geometry(GeometryDataType type)
        {
            var goType = _types.Build(type.Element).TrimStart('*');
            return $"{type.Kind}Builder[{(type.Nullable ? $"*{goType}" : goType)}]()";
        }

        protected override string ByteType(DataType type) => Default(type, "uint8");
        protected override string SbyteType(DataType type) => Default(type, "int8");
        protected override string ShortType(DataType type) => Default(type, "int16");
        protected override string UshortType(DataType type) => Default(type, "uint16");
        protected override string BoolType(DataType type) => Default(type, type.Root);
        protected override string IntType(DataType type) => Default(type, "int32");
        protected override string UintType(DataType type) => Default(type, "uint32");
        protected override string LongType(DataType type) => Default(type, "int64");
        protected override string UlongType(DataType type) => Default(type, "uint64");
        protected override string DoubleType(DataType type) => Default(type, type.Root);
        protected override string FloatType(DataType type) => Default(type, "float32");
        protected override string StringType(DataType type) => "DefaultBuilder[string]()";
        protected override string DslType(DataType type) => "DslBuilder()";
        protected override string TimeSpanType(DataType type) => "TimeSpanBuilder()";
        protected override string DateTimeType(DataType type) => "DateTimeBuilder()";
        protected override string DateRangeType(DataType type) => "DateRangeBuilder()";
        protected override string ArrayType(ArrayDataType type) => $"ArrayBuilder({Func(type.Element, string.Empty)})";
        protected override string MapType(MapDataType type) => $"DictionaryBuilder({Func(type.Key, "     ")}, {Func(type.Value, string.Empty)})";
        protected override string PointType(GeometryDataType type) => Geometry(type);
        protected override string SizeType(GeometryDataType type) => Geometry(type);
        protected override string RangeType(GeometryDataType type) => Geometry(type);
        protected override string AreaType(GeometryDataType type) => Geometry(type);
        protected override string EnumType(EnumDataType type) => $"EnumBuilder[{type.Naked}]({type.Root.ToLower()}_name_to_value)";
    }
}
