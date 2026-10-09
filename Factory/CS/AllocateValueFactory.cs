using ExcelTableConverter.Model;
using Newtonsoft.Json.Linq;

namespace ExcelTableConverter.Factory.CS
{
    public class AllocateValueFactory : ValueFormatFactory<string>
    {
        private readonly TypeFactory _types;

        public AllocateValueFactory(Context ctx) : base(ctx)
        {
            _types = new TypeFactory(ctx);
        }

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
                return "string.Empty";
            else if (s.Contains('\n'))
                return $"@\"{s}\"";
            else
                return $"\"{s}\"";
        }

        protected override string DslType(DataType type, DslValue value, IExcelFileTrackable tracker)
        {
            if (Context.DSL.TryGetValue(value.Header, out var prototype) == false)
                throw new LogicException($"{value.Header}는 정의되지 않은 DSL 형식입니다.", tracker);

            var args = value.Params.Select((x, i) =>
            {
                var param = (prototype as JArray).ElementAt(i) as JObject;
                return $"{param["name"].Value<string>()} = {Build(param["type"].Value<string>(), x)}";
            });
            return $"new MasterData.Types.Dsl.Parameter.{value.Header} {{ {string.Join(", ", args)} }}.ToDsl()";
        }

        protected override string TimeSpanType(DataType type, TimeSpanValue value, IExcelFileTrackable tracker)
        {
            return $"TimeSpan.Parse({Build("string", new StringValue(value.Value.ToString()))})";
        }

        protected override string DateTimeType(DataType type, DateTimeValue value, IExcelFileTrackable tracker)
        {
            return $"DateTime.Parse({Build("string", new StringValue(value.Value.ToString("yyyy-MM-dd HH:mm:ss")))})";
        }

        protected override string DateRangeType(DataType type, DateRangeValue value, IExcelFileTrackable tracker)
        {
            return $"new DateRange {{ Begin = {Build("DateTime", value.Begin)}, End = {Build("DateTime", value.End)} }}";
        }

        protected override string ArrayType(ArrayDataType type, ArrayValue value, IExcelFileTrackable tracker)
        {
            return $"new {_types.Build(type)} {{ {string.Join(", ", value.Items.Select(x => Build(type.Element, x)))} }}";
        }

        protected override string MapType(MapDataType type, MapValue value, IExcelFileTrackable tracker)
        {
            var values = string.Join(", ", value.Entries.Select(x => $"[{Build(type.Key, x.Key)}] = {Build(type.Value, x.Value)}"));
            return $"new Dictionary<{_types.Build(type.Key)}, {_types.Build(type.Value)}> {{ {values} }}";
        }

        protected override string PointType(GeometryDataType type, PointValue value, IExcelFileTrackable tracker)
        {
            return $"new Point<{_types.Build(type.Element)}> {{ X = {value.X}, Y = {value.Y} }}";
        }

        protected override string SizeType(GeometryDataType type, SizeValue value, IExcelFileTrackable tracker)
        {
            return $"new Size<{_types.Build(type.Element)}> {{ Width = {value.Width}, Height = {value.Height} }}";
        }

        protected override string RangeType(GeometryDataType type, RangeValue value, IExcelFileTrackable tracker)
        {
            return $"new Range<{_types.Build(type.Element)}> {{ Min = {value.Min}, Max = {value.Max} }}";
        }

        protected override string AreaType(GeometryDataType type, AreaValue value, IExcelFileTrackable tracker)
        {
            return $"new Area<{_types.Build(type.Element)}> {{ Left = {value.Left}, Top = {value.Top}, Right = {value.Right}, Bottom = {value.Bottom} }}";
        }

        protected override string EnumType(EnumDataType type, EnumValue value, IExcelFileTrackable tracker)
        {
            if (value.Name == null)
                return $"{value}";

            return $"{Language.CSharp.Identifier(type.Naked)}.{Language.CSharp.Identifier(value.Name)}";
        }
    }
}
