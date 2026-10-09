using ExcelTableConverter.Model;
using Newtonsoft.Json.Linq;

namespace ExcelTableConverter.Factory.Node
{
    public class AllocateValueFactory : ValueFormatFactory<string>
    {
        public AllocateValueFactory(Context ctx) : base(ctx)
        { }

        protected override string Null(DataType type, IExcelFileTrackable tracker) => "null";
        protected override string ByteType(DataType type, UnsignedValue value, IExcelFileTrackable tracker) => $"{value}";
        protected override string SbyteType(DataType type, IntegerValue value, IExcelFileTrackable tracker) => $"{value}";
        protected override string ShortType(DataType type, IntegerValue value, IExcelFileTrackable tracker) => $"{value}";
        protected override string UshortType(DataType type, UnsignedValue value, IExcelFileTrackable tracker) => $"{value}";
        protected override string BoolType(DataType type, BoolValue value, IExcelFileTrackable tracker) => value.Value ? "true" : "false";
        protected override string IntType(DataType type, IntegerValue value, IExcelFileTrackable tracker) => $"{value}";
        protected override string UintType(DataType type, UnsignedValue value, IExcelFileTrackable tracker) => $"{value}";
        protected override string LongType(DataType type, IntegerValue value, IExcelFileTrackable tracker) => $"{value}";
        protected override string UlongType(DataType type, UnsignedValue value, IExcelFileTrackable tracker) => $"{value}";
        protected override string DoubleType(DataType type, RealValue value, IExcelFileTrackable tracker) => $"{value}";
        protected override string FloatType(DataType type, RealValue value, IExcelFileTrackable tracker) => $"{value}";

        protected override string StringType(DataType type, StringValue value, IExcelFileTrackable tracker)
        {
            var s = value.Value;
            if (string.IsNullOrEmpty(s))
                return "null";
            else if (s.Contains('\n'))
                return $"`{s}`";
            else
                return $"\"{s}\"";
        }

        protected override string DslType(DataType type, DslValue value, IExcelFileTrackable tracker)
        {
            if (Context.DSL.TryGetValue(value.Header, out var prototype) == false)
                throw new LogicException($"{value.Header}는 정의되지 않은 DSL 형식입니다.");

            var args = value.Params.Select((x, i) =>
            {
                var param = (prototype as JArray).ElementAt(i) as JObject;
                return $"{param["name"].Value<string>()} = {Build(param["type"].Value<string>(), x, tracker)}";
            });
            return $"new MasterData.Types.Dsl.Parameter.{value.Header} {{ {string.Join(", ", args)} }}.ToDsl()";
        }

        protected override string TimeSpanType(DataType type, TimeSpanValue value, IExcelFileTrackable tracker)
        {
            var ts = value.Value;
            var ms = (long)ts.TotalMilliseconds;
            if (ms == 0)
                return "new timespan.TimeSpan()";
            else
                return $"timespan.fromMilliseconds({ms}/*{ts}*/)";
        }

        protected override string DateTimeType(DataType type, DateTimeValue value, IExcelFileTrackable tracker)
        {
            return $"date.parse({Build("string", new StringValue(value.Value.ToString("yyyy-MM-dd HH:mm:ss")), tracker)}, 'YYYY-MM-DD HH:mm:ss')";
        }

        protected override string DateRangeType(DataType type, DateRangeValue value, IExcelFileTrackable tracker)
        {
            return $"{{ Begin: {Build("DateTime", value.Begin, tracker)}, End: {Build("DateTime", value.End, tracker)} }}";
        }

        protected override string ArrayType(ArrayDataType type, ArrayValue value, IExcelFileTrackable tracker)
        {
            return $"[{string.Join(", ", value.Items.Select(x => Build(type.Element, x, tracker)))}]";
        }

        protected override string MapType(MapDataType type, MapValue value, IExcelFileTrackable tracker)
        {
            return $"{{ {string.Join(", ", value.Entries.Select(x => $"[{Build(type.Key, x.Key, tracker)}]: {Build(type.Value, x.Value, tracker)}"))} }}";
        }

        protected override string PointType(GeometryDataType type, PointValue value, IExcelFileTrackable tracker)
        {
            return $"{{ x: {value.X}, y: {value.Y} }}";
        }

        protected override string SizeType(GeometryDataType type, SizeValue value, IExcelFileTrackable tracker)
        {
            return $"{{ width: {value.Width}, height: {value.Height} }}";
        }

        protected override string RangeType(GeometryDataType type, RangeValue value, IExcelFileTrackable tracker)
        {
            return $"{{ min: {value.Min}, max: {value.Max} }}";
        }

        protected override string AreaType(GeometryDataType type, AreaValue value, IExcelFileTrackable tracker)
        {
            return $"{{ left: {value.Left}, top: {value.Top}, right: {value.Right}, bottom: {value.Bottom} }}";
        }

        protected override string EnumType(EnumDataType type, EnumValue value, IExcelFileTrackable tracker)
        {
            if (value.Name == null)
                return $"{value}";

            return $"$enum.{type.Naked}.{value.Name}";
        }
    }
}
