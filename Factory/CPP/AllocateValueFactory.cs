using ExcelTableConverter.Model;
using Newtonsoft.Json.Linq;

namespace ExcelTableConverter.Factory.CPP
{
    public class AllocateValueFactory : ValueFormatFactory<string>
    {
        public AllocateValueFactory(Context ctx) : base(ctx)
        { }

        protected override string Null(DataType type, IExcelFileTrackable tracker) => "std::nullopt";
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
                return $"R\"({s})\"";
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
            return $"timespan({Build("string", new StringValue(value.Value.ToString("dd\\.hh\\:mm\\:ss")))})";
        }

        protected override string DateTimeType(DataType type, DateTimeValue value, IExcelFileTrackable tracker)
        {
            return $"datetime({Build("string", new StringValue(value.Value.ToString("yyyy-MM-dd HH:mm:ss")))})";
        }

        protected override string DateRangeType(DataType type, DateRangeValue value, IExcelFileTrackable tracker)
        {
            return $"{{ {Build("DateTime", value.Begin)}, {Build("DateTime", value.End)} }}";
        }

        protected override string ArrayType(ArrayDataType type, ArrayValue value, IExcelFileTrackable tracker)
        {
            return $"{{ {string.Join(", ", value.Items.Select(x => Build(type.Element, x)))} }}";
        }

        protected override string MapType(MapDataType type, MapValue value, IExcelFileTrackable tracker)
        {
            return $"{{ {string.Join(", ", value.Entries.Select(x => $"{{{Build(type.Key, x.Key)}, {Build(type.Value, x.Value)}}}"))} }}";
        }

        protected override string PointType(GeometryDataType type, PointValue value, IExcelFileTrackable tracker)
        {
            return $"point<{type.Element.Naked}>({value.X}, {value.Y})";
        }

        protected override string SizeType(GeometryDataType type, SizeValue value, IExcelFileTrackable tracker)
        {
            return $"size<{type.Element.Naked}>({value.Width}, {value.Height})";
        }

        protected override string RangeType(GeometryDataType type, RangeValue value, IExcelFileTrackable tracker)
        {
            return $"range<{type.Element.Naked}>({value.Min}, {value.Max})";
        }

        protected override string AreaType(GeometryDataType type, AreaValue value, IExcelFileTrackable tracker)
        {
            return $"area<{type.Element.Naked}>({value.Left}, {value.Top}, {value.Right}, {value.Bottom})";
        }

        protected override string EnumType(EnumDataType type, EnumValue value, IExcelFileTrackable tracker)
        {
            if (value.Name == null)
                return $"{value}";

            return $"{Language.Cpp.Qualify(Context.EnumNamespace, type.Naked)}::{value.Name}";
        }
    }
}
