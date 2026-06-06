using ExcelTableConverter.Model;
using Newtonsoft.Json.Linq;

namespace ExcelTableConverter.Factory.Go
{
    public class AllocateValueFactory : DataFormatFactory<string>
    {
        public AllocateValueFactory(Context ctx) : base(ctx)
        {

        }

        protected override bool OnStart(object value, string root, bool nullable, out string result, DataFormatOption option, IExcelFileTrackable tracker)
        {
            if (Util.Value.IsNull(value))
            {
                result = "nil";
                return false;
            }

            result = string.Empty;
            return true;
        }

        protected override string ArrayType(object value, string root, string e, DataFormatOption option, IExcelFileTrackable tracker)
        {
            var list = value as List<object>;
            var values = string.Join(", ", list.Select(x => Build(e, x, option, tracker)));
            return $"[]{Build(e, null, option)}{{{values}}}";
        }

        protected override string BooleanType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return value.ToString().ToLower();
        }

        protected override string DateRangeType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            var range = value as DateRange;
            return $"DateRange{{Begin: {Build("DateTime", range.Begin, option, tracker)}, End: {Build("DateTime", range.End, option, tracker)}}}";
        }

        protected override string DateTimeType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            if (value is DateTime dt)
            {
                return $"{dt.Ticks / TimeSpan.TicksPerMillisecond} * time.Millisecond";
            }
            else if (value is TimeSpan ts)
            {
                return $"{ts.TotalMilliseconds} * time.Millisecond";
            }
            throw new InvalidOperationException($"Unexpected type for DateTimeType: {value.GetType()}");
        }

        protected override string DictionaryType(object value, string root, string k, string v, DataFormatOption option, IExcelFileTrackable tracker)
        {
            var dict = value as Dictionary<object, object>;
            var pairs = dict.Select(x => $"{Build(k, x.Key, option, tracker)}: {Build(v, x.Value, option, tracker)}");
            return $"map[{Build(k, null, option, tracker)}]{Build(v, null, option, tracker)}{{{string.Join(", ", pairs)}}}";
        }

        protected override string FloatType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return value.ToString();
        }

        protected override string DslType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            var dsl = value as DSL;

            if (Context.DSL.TryGetValue(dsl.Header, out var prototype) == false)
                throw new LogicException($"{dsl.Header}는 정의되지 않은 DSL 형식입니다.");

            var args = dsl.Params.Select((x, i) =>
            {
                var param = (prototype as JArray).ElementAt(i) as JObject;
                var name = param["name"].Value<string>();
                var type = param["type"].Value<string>();
                return (Name: name, Value: Build(type, x, option, tracker));
            }).ToList();

            var typeName = char.ToUpper(dsl.Header[0]) + dsl.Header.Substring(1) + "Dsl";
            return $"{typeName}{{ {string.Join(", ", args.Select(x => $"{char.ToUpper(x.Name[0]) + x.Name.Substring(1)}: {x.Value}"))} }}";
        }

        protected override string EnumType(object value, string root, string e, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            switch (value)
            {
                case string s:
                    {
                        if (Context.Completed.Enum[root].ContainsKey(s) == false)
                            throw new LogicException($"{value}는 {root} 열거형에 존재하지 않는 값입니다.");
                        return $"{root}.{s}";
                    }

                case int i:
                    {
                        return i.ToString();
                    }

                default:
                    throw new LogicException($"{value}는 {root} 열거형에 존재하지 않는 값입니다.");
            }
        }

        protected override string IntType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return value.ToString();
        }

        protected override string LongType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return value.ToString();
        }

        protected override string ByteType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return value.ToString();
        }

        protected override string SbyteType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return value.ToString();
        }

        protected override string ShortType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return value.ToString();
        }

        protected override string UshortType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return value.ToString();
        }

        protected override string UintType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return value.ToString();
        }

        protected override string UlongType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return value.ToString();
        }

        protected override string StringType(object value, string root, DataFormatOption option, IExcelFileTrackable tracker)
        {
            var s = value as string;
            if (string.IsNullOrEmpty(s))
                return "\"\"";
            else if (s.Contains('\n'))
                return $"`{s}`";
            else
                return $"\"{s}\"";
        }

        protected override string TimeSpanType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            var timeSpan = (TimeSpan)value;
            return $"{timeSpan.TotalMilliseconds} * time.Millisecond";
        }

        protected override string PointType(object value, string root, string e, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            var point = value as Point;
            return $"Point[{Build(e, null, option, tracker)}]{{X: {point.X}, Y: {point.Y}}}";
        }

        protected override string SizeType(object value, string root, string e, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            var size = value as Size;
            return $"Size[{Build(e, null, option, tracker)}]{{Width: {size.Width}, Height: {size.Height}}}";
        }

        protected override string RangeType(object value, string root, string e, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            var range = value as ExcelTableConverter.Model.Range;
            return $"Range[{Build(e, null, option, tracker)}]{{Min: {range.Min}, Max: {range.Max}}}";
        }

        protected override string AreaType(object value, string root, string e, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            var area = value as Area;
            return $"Area{{ Left: {area.Left}, Top: {area.Top}, Right: {area.Right}, Bottom: {area.Bottom} }}";
        }

        protected override string DoubleType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return value.ToString();
        }

        public string Build(string type, object value, IExcelFileTrackable tracker = null)
        {
            return base.Build(type, value, null, tracker);
        }

        public new string Build(string type, object value, DataFormatOption option, IExcelFileTrackable tracker = null)
        {
            return base.Build(type, value, option, tracker);
        }
    }
}
