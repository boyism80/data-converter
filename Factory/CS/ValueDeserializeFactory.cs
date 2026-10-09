using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory.CS
{
    public class ValueDeserializeFactory : ExpressionFormatFactory<string>
    {
        public ValueDeserializeFactory(Context ctx) : base(ctx)
        { }

        private static string Nullable(DataType type, string obj, string result)
        {
            return type.Nullable ? $"{obj} == null ? null : {result}" : result;
        }

        private static string Cast(DataType type, string obj, string name, string expr)
        {
            return Nullable(type, obj, type.Nullable ? $"({name}?){expr}" : expr);
        }

        protected override string ByteType(DataType type, string obj) => Cast(type, obj, "byte", $"(byte){obj}");
        protected override string SbyteType(DataType type, string obj) => Cast(type, obj, "ubyte", $"(ubyte){obj}");
        protected override string ShortType(DataType type, string obj) => Cast(type, obj, "short", $"(short){obj}");
        protected override string UshortType(DataType type, string obj) => Cast(type, obj, "ushort", $"(ushort){obj}");
        protected override string IntType(DataType type, string obj) => Cast(type, obj, "int", $"(int)(long){obj}");
        protected override string UintType(DataType type, string obj) => Cast(type, obj, "uint", $"(uint){obj}");
        protected override string LongType(DataType type, string obj) => Cast(type, obj, "long", $"(long){obj}");
        protected override string UlongType(DataType type, string obj) => Cast(type, obj, "ulong", $"(ulong){obj}");
        protected override string DoubleType(DataType type, string obj) => Cast(type, obj, "double", $"(double){obj}");
        protected override string FloatType(DataType type, string obj) => Cast(type, obj, "float", $"(float)(double){obj}");
        protected override string DslType(DataType type, string obj) => Cast(type, obj, "Dsl", $"Newtonsoft.Json.JsonConvert.DeserializeObject<Dsl>(Newtonsoft.Json.JsonConvert.SerializeObject({obj}))");
        protected override string TimeSpanType(DataType type, string obj) => Cast(type, obj, "TimeSpan", $"TimeSpan.Parse({obj}.ToString())");
        protected override string BoolType(DataType type, string obj) => Nullable(type, obj, $"({new TypeFactory(Context).Build(type.Root)})System.Convert.ChangeType({obj}, typeof({type.Root}))");
        protected override string DateTimeType(DataType type, string obj) => Nullable(type, obj, $"DateTime.Parse({obj}.ToString())");
        protected override string DateRangeType(DataType type, string obj) => Nullable(type, obj, $"DateRange.Parse({obj})");
        protected override string StringType(DataType type, string obj) => $"{obj}?.ToString()";
        protected override string ArrayType(ArrayDataType type, string obj) => $"({obj} as object[]).Select(x => {Build(type.Element, "x")}).ToList()";
        protected override string MapType(MapDataType type, string obj) => $"({obj} as object[]).Select(x => {Build(type.Key, "x")}).ToList()";
        protected override string PointType(GeometryDataType type, string obj) => throw new NotImplementedException();
        protected override string SizeType(GeometryDataType type, string obj) => throw new NotImplementedException();
        protected override string RangeType(GeometryDataType type, string obj) => throw new NotImplementedException();
        protected override string AreaType(GeometryDataType type, string obj) => throw new NotImplementedException();

        protected override string EnumType(EnumDataType type, string obj)
        {
            var enumType = Language.CSharp.Qualify(Context.EnumNamespace, type.Root);
            return Nullable(type, obj, $"({enumType})Enum.Parse(typeof({enumType}), {obj}.ToString())");
        }
    }
}
