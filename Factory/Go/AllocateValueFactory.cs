using ExcelTableConverter.Model;
using Newtonsoft.Json.Linq;

namespace ExcelTableConverter.Factory.Go
{
    public class AllocateValueFactory : ValueFormatFactory<string>
    {
        private readonly TypeFactory _types;

        public AllocateValueFactory(Context ctx) : base(ctx)
        {
            _types = new TypeFactory(ctx);
        }

        protected override string Null(DataType type, IExcelFileTrackable tracker) => "nil";
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
                return "\"\"";
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
                var name = param["name"].Value<string>();
                return $"{char.ToUpper(name[0]) + name.Substring(1)}: {Build(param["type"].Value<string>(), x, tracker)}";
            });
            var typeName = char.ToUpper(value.Header[0]) + value.Header.Substring(1) + "Dsl";
            return $"{typeName}{{ {string.Join(", ", args)} }}";
        }

        protected override string TimeSpanType(DataType type, TimeSpanValue value, IExcelFileTrackable tracker)
        {
            return $"{value.Value.TotalMilliseconds} * time.Millisecond";
        }

        protected override string DateTimeType(DataType type, DateTimeValue value, IExcelFileTrackable tracker)
        {
            return $"{value.Value.Ticks / TimeSpan.TicksPerMillisecond} * time.Millisecond";
        }

        protected override string DateRangeType(DataType type, DateRangeValue value, IExcelFileTrackable tracker)
        {
            return $"DateRange{{Begin: {Build("DateTime", value.Begin, tracker)}, End: {Build("DateTime", value.End, tracker)}}}";
        }

        protected override string ArrayType(ArrayDataType type, ArrayValue value, IExcelFileTrackable tracker)
        {
            return $"[]{_types.Build(type.Element)}{{{string.Join(", ", value.Items.Select(x => Build(type.Element, x, tracker)))}}}";
        }

        protected override string MapType(MapDataType type, MapValue value, IExcelFileTrackable tracker)
        {
            var pairs = value.Entries.Select(x => $"{Build(type.Key, x.Key, tracker)}: {Build(type.Value, x.Value, tracker)}");
            return $"map[{_types.Build(type.Key)}]{_types.Build(type.Value)}{{{string.Join(", ", pairs)}}}";
        }

        protected override string PointType(GeometryDataType type, PointValue value, IExcelFileTrackable tracker)
        {
            return $"Point[{_types.Build(type.Element)}]{{X: {value.X}, Y: {value.Y}}}";
        }

        protected override string SizeType(GeometryDataType type, SizeValue value, IExcelFileTrackable tracker)
        {
            return $"Size[{_types.Build(type.Element)}]{{Width: {value.Width}, Height: {value.Height}}}";
        }

        protected override string RangeType(GeometryDataType type, RangeValue value, IExcelFileTrackable tracker)
        {
            return $"Range[{_types.Build(type.Element)}]{{Min: {value.Min}, Max: {value.Max}}}";
        }

        protected override string AreaType(GeometryDataType type, AreaValue value, IExcelFileTrackable tracker)
        {
            return $"Area{{ Left: {value.Left}, Top: {value.Top}, Right: {value.Right}, Bottom: {value.Bottom} }}";
        }

        protected override string EnumType(EnumDataType type, EnumValue value, IExcelFileTrackable tracker)
        {
            if (value.Name == null)
                return $"{value}";

            return $"{type.Naked}.{value.Name}";
        }
    }
}
