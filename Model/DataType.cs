using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace ExcelTableConverter.Model
{
    public enum DataKind
    {
        Byte,
        Sbyte,
        Short,
        Ushort,
        Bool,
        Int,
        Uint,
        Long,
        Ulong,
        Double,
        Float,
        String,
        Dsl,
        TimeSpan,
        DateTime,
        DateRange,
        Array,
        Map,
        Point,
        Size,
        Range,
        Area,
        Enum,
    }

    // Root is the resolved type (relations replaced by the referenced key type), Naked is Root without "?".
    public abstract record DataType(DataKind Kind, string Root, string Naked, bool Nullable)
    {
        private static readonly Regex _array = new Regex(@"^\[(?<type>.*)\]$", RegexOptions.Compiled);
        private static readonly Regex _map = new Regex(@"^\s*{\s*(?<key>[.\S]+)\s*:\s*(?<value>[.\S]+)\s*}\s*$", RegexOptions.Compiled);
        private static readonly Regex _geometry = new Regex(@"^(?<kind>point|size|range|area)(<(?<e>.+)>)?$", RegexOptions.Compiled);
        private static readonly Regex _const = new Regex(@"^Const:(?<table>[a-zA-Z_]+):(?<name>[a-zA-Z_]+)", RegexOptions.Compiled);

        public static bool IsArray(string type, out string element)
        {
            var match = _array.Match(type);
            element = match.Success ? match.Groups["type"].Value : string.Empty;
            return match.Success;
        }

        public static bool IsMap(string type, out string key, out string value)
        {
            var match = _map.Match(type);
            key = match.Success ? match.Groups["key"].Value : null;
            value = match.Success ? match.Groups["value"].Value : null;
            return match.Success;
        }

        // Use Context.Type, which caches the parsed tree per type string.
        public static DataType Parse(Context ctx, string type, IExcelFileTrackable tracker = null)
        {
            var root = ctx.Completed.Schema.RootType(type);
            var column = ColumnType.Parse(root);
            var naked = column.Naked;
            var nullable = column.Nullable;

            if (ColumnType.Parse(type).Sequence)
                return ctx.Type(nullable ? "int?" : "int", tracker);

            var geometry = _geometry.Match(naked);
            switch (naked)
            {
                case "byte" or "uint8" or "uint8_t":
                    return new UnsignedDataType(DataKind.Byte, root, naked, nullable, byte.MaxValue);
                case "ushort" or "uint16" or "uint16_t":
                    return new UnsignedDataType(DataKind.Ushort, root, naked, nullable, ushort.MaxValue);
                case "uint" or "uint32" or "uint32_t":
                    return new UnsignedDataType(DataKind.Uint, root, naked, nullable, uint.MaxValue);
                case "ulong" or "uint64" or "uint64_t":
                    return new UnsignedDataType(DataKind.Ulong, root, naked, nullable, ulong.MaxValue);
                case "sbyte" or "int8" or "int8_t":
                    return new SignedDataType(DataKind.Sbyte, root, naked, nullable, sbyte.MinValue, sbyte.MaxValue);
                case "short" or "int16" or "int16_t":
                    return new SignedDataType(DataKind.Short, root, naked, nullable, short.MinValue, short.MaxValue);
                case "int" or "int32" or "int32_t":
                    return new SignedDataType(DataKind.Int, root, naked, nullable, int.MinValue, int.MaxValue);
                case "long" or "int64" or "int64_t":
                    return new SignedDataType(DataKind.Long, root, naked, nullable, long.MinValue, long.MaxValue);
                case "double" or "float64":
                    return new RealDataType(DataKind.Double, root, naked, nullable, double.MinValue, double.MaxValue);
                case "float" or "float32":
                    return new RealDataType(DataKind.Float, root, naked, nullable, float.MinValue, float.MaxValue);
                case "bool":
                    return new BoolDataType(root, naked, nullable);
                case "string":
                    return new StringDataType(root, naked, nullable);
                case "cron":
                    return new CronDataType(root, naked, nullable);
                case "dsl":
                    return new DslDataType(root, naked, nullable);
                case "TimeSpan":
                    return new TimeSpanDataType(root, naked, nullable);
                case "DateTime":
                    return new DateTimeDataType(root, naked, nullable);
                case "DateRange":
                    return new DateRangeDataType(root, naked, nullable);
            }

            if (IsArray(naked, out var e))
            {
                return new ArrayDataType(root, naked, nullable, ctx.Type(e, tracker));
            }
            else if (IsMap(naked, out var key, out var value))
            {
                return new MapDataType(root, naked, nullable, ctx.Type(key, tracker), ctx.Type(value, tracker));
            }
            else if (geometry.Success)
            {
                var geometryKind = geometry.Groups["kind"].Value switch
                {
                    "point" => DataKind.Point,
                    "size" => DataKind.Size,
                    "range" => DataKind.Range,
                    _ => DataKind.Area,
                };
                var element = geometry.Groups["e"].Value;
                return new GeometryDataType(geometryKind, root, naked, nullable, ctx.Type(string.IsNullOrEmpty(element) ? "uint" : element, tracker));
            }
            else if (ctx.Completed.Enum.ContainsKey(naked))
            {
                return new EnumDataType(root, naked, nullable);
            }
            else
            {
                throw new LogicException($"{naked} 타입은 정의되지 않은 타입 형식입니다.", tracker);
            }
        }

        // raw is a cell as loaded from the sheet: string, long, bool, TimeSpan or DateTime, or null.
        public DataValue Cast(Context ctx, object raw, IExcelFileTrackable tracker = null)
        {
            if (raw is string s && _const.Match(s) is { Success: true } match)
            {
                var table = match.Groups["table"].Value;
                var name = match.Groups["name"].Value;
                var consts = ctx.Source.Const.Values.SelectMany(x => x).Where(x => x.TableName == table).ToList();
                if (consts.Count == 0)
                    throw new LogicException($"{table}은 상수 테이블에 정의되지 않았습니다.", tracker);

                var source = consts.FirstOrDefault(x => x.Name == name) ?? throw new LogicException($"{name}은 {table}에 정의되지 않았습니다.", tracker);
                return Cast(ctx, source.Value, tracker);
            }

            if (raw == null || (raw is string text && (text.Length == 0 || text.Trim() == "null")))
                return Null();

            return ctx.CachedValue(Root, raw, () => From(ctx, raw, tracker));
        }

        protected virtual DataValue Null()
        {
            if (Nullable == false)
                throw new NullValueException(Root);

            return NullValue.Instance;
        }

        protected abstract DataValue From(Context ctx, object raw, IExcelFileTrackable tracker);
    }

    public sealed record SignedDataType(DataKind Kind, string Root, string Naked, bool Nullable, long Min, long Max) : DataType(Kind, Root, Naked, Nullable)
    {
        protected override DataValue From(Context ctx, object raw, IExcelFileTrackable tracker)
        {
            long value;
            switch (raw)
            {
                case long v:
                    value = v;
                    break;

                case string s:
                    try
                    {
                        value = s.StartsWith("0x") ? Convert.ToInt64(s, 16) : long.Parse(s);
                    }
                    catch (FormatException)
                    {
                        throw new LogicException($"{s}는 {Root} 타입의 형식이 올바르지 않습니다.", tracker);
                    }
                    catch (OverflowException)
                    {
                        throw new LogicException($"{s}는 {Root} 타입의 범위를 초과합니다.", tracker);
                    }
                    break;

                default:
                    if (long.TryParse($"{raw}", out value) == false)
                        throw new TypeCastException(raw, Root);
                    break;
            }

            if (value < Min)
                throw new LogicException($"{value}는 {Root} 타입의 최소값보다 작은 값입니다.", tracker);

            if (value > Max)
                throw new LogicException($"{value}는 {Root} 타입의 최대값보다 큰 값입니다.", tracker);

            return new IntegerValue(value);
        }
    }

    public sealed record UnsignedDataType(DataKind Kind, string Root, string Naked, bool Nullable, ulong Max) : DataType(Kind, Root, Naked, Nullable)
    {
        protected override DataValue From(Context ctx, object raw, IExcelFileTrackable tracker)
        {
            ulong value;
            switch (raw)
            {
                case long v:
                    if (v < 0)
                        throw new LogicException($"{v}는 {Root} 타입의 최소값보다 작은 값입니다.", tracker);

                    value = (ulong)v;
                    break;

                case string s:
                    try
                    {
                        value = s.StartsWith("0x") ? Convert.ToUInt64(s, 16) : ulong.Parse(s);
                    }
                    catch (FormatException)
                    {
                        throw new LogicException($"{s}는 {Root} 타입의 형식이 올바르지 않습니다.", tracker);
                    }
                    catch (OverflowException)
                    {
                        throw new LogicException($"{s}는 {Root} 타입의 범위를 초과합니다.", tracker);
                    }
                    break;

                default:
                    if (ulong.TryParse($"{raw}", out value) == false)
                        throw new TypeCastException(raw, Root);
                    break;
            }

            if (value > Max)
                throw new LogicException($"{value}는 {Root} 타입의 최대값보다 큰 값입니다.", tracker);

            return new UnsignedValue(value);
        }
    }

    public sealed record RealDataType(DataKind Kind, string Root, string Naked, bool Nullable, double Min, double Max) : DataType(Kind, Root, Naked, Nullable)
    {
        protected override DataValue From(Context ctx, object raw, IExcelFileTrackable tracker)
        {
            double value;
            if (raw is long l)
                value = l;
            else if (double.TryParse($"{raw}", out value) == false)
                throw new TypeCastException(raw, Root);

            if (value < Min)
                throw new LogicException($"{value}는 {Root} 타입의 최소값보다 작은 값입니다.", tracker);

            if (value > Max)
                throw new LogicException($"{value}는 {Root} 타입의 최대값보다 큰 값입니다.", tracker);

            return new RealValue(value);
        }
    }

    public sealed record BoolDataType(string Root, string Naked, bool Nullable) : DataType(DataKind.Bool, Root, Naked, Nullable)
    {
        protected override DataValue From(Context ctx, object raw, IExcelFileTrackable tracker)
        {
            if (raw is bool b)
                return new BoolValue(b);

            if (bool.TryParse($"{raw}".ToLower(), out var result) == false)
                throw new TypeCastException(raw, Root);

            return new BoolValue(result);
        }
    }

    public sealed record StringDataType(string Root, string Naked, bool Nullable) : DataType(DataKind.String, Root, Naked, Nullable)
    {
        // An empty string cell is null even when the type is not nullable.
        protected override DataValue Null() => NullValue.Instance;

        protected override DataValue From(Context ctx, object raw, IExcelFileTrackable tracker) => new StringValue($"{raw}");
    }

    public sealed record DslDataType(string Root, string Naked, bool Nullable) : DataType(DataKind.Dsl, Root, Naked, Nullable)
    {
        private static readonly Regex _dsl = new Regex(@"^(?<header>\w+)\((?<parameters>.*)\)$", RegexOptions.Compiled);

        protected override DataValue From(Context ctx, object raw, IExcelFileTrackable tracker)
        {
            var match = raw is string s ? _dsl.Match(s) : Match.Empty;
            if (match.Success == false)
                throw new LogicException($"{raw}는 DSL로 변환할 수 없습니다.", tracker);

            var header = match.Groups["header"].Value;
            var parameters = match.Groups["parameters"].Value.Split(',', StringSplitOptions.TrimEntries).Where(x => string.IsNullOrEmpty(x) == false).ToList();
            if (ctx.DSL.TryGetValue(header, out var prototype) == false)
                throw new LogicException($"{header}는 정의되지 않은 dsl입니다.", tracker);

            var definedParams = (prototype as JArray).Cast<JObject>().ToList();
            var essentialCount = definedParams.Count(x => x.ContainsKey("default") == false);
            if (parameters.Count < essentialCount)
                throw new LogicException($"{raw} 형식이 올바르지 않습니다. {header}는 최소 {essentialCount}의 인자가 필요합니다. ", tracker);

            if (parameters.Count > definedParams.Count)
                throw new LogicException($"{raw} 형식이 올바르지 않습니다. {header}는 최대 {definedParams.Count}의 인자만 받습니다.", tracker);

            var values = new List<DataValue>();
            for (int i = 0; i < definedParams.Count; i++)
            {
                var param = parameters.ElementAtOrDefault(i);
                if (param == null)
                {
                    if (definedParams[i].TryGetValue("default", out var defaultValue) == false)
                        throw new LogicException($"{header}의 {i + 1}번째 파라미터 {definedParams[i]["name"]}은 디폴트로 정의할 수 없습니다.", tracker);

                    param = defaultValue.Value<string>();
                }

                values.Add(ctx.Cast(definedParams[i]["type"].Value<string>(), param, tracker));
            }

            return new DslValue(header, values);
        }
    }

    public sealed record TimeSpanDataType(string Root, string Naked, bool Nullable) : DataType(DataKind.TimeSpan, Root, Naked, Nullable)
    {
        protected override DataValue From(Context ctx, object raw, IExcelFileTrackable tracker)
        {
            switch (raw)
            {
                case TimeSpan ts:
                    return new TimeSpanValue(ts);

                case string s when TimeSpan.TryParse(s.Replace(' ', '.'), out var result):
                    return new TimeSpanValue(result);

                default:
                    throw new TypeCastException(raw, Root);
            }
        }
    }

    public sealed record DateTimeDataType(string Root, string Naked, bool Nullable) : DataType(DataKind.DateTime, Root, Naked, Nullable)
    {
        protected override DataValue From(Context ctx, object raw, IExcelFileTrackable tracker)
        {
            switch (raw)
            {
                case DateTime dt:
                    return new DateTimeValue(dt);

                case string s when DateTime.TryParse(s, out var result):
                    return new DateTimeValue(result);

                default:
                    throw new TypeCastException(raw, Root);
            }
        }
    }

    // "begin~end", either side may be empty; a single date has no end.
    public sealed record DateRangeDataType(string Root, string Naked, bool Nullable) : DataType(DataKind.DateRange, Root, Naked, Nullable)
    {
        protected override DataValue From(Context ctx, object raw, IExcelFileTrackable tracker)
        {
            if (raw is not string s)
                throw new TypeCastException(raw, Root);

            var dateTime = ctx.Type("DateTime");
            if (s.Contains('~'))
            {
                var split = s.Split('~', StringSplitOptions.TrimEntries);
                var begin = split.Length > 0 && string.IsNullOrWhiteSpace(split[0]) == false ? dateTime.Cast(ctx, split[0], tracker) : NullValue.Instance;
                var end = split.Length > 1 && string.IsNullOrWhiteSpace(split[1]) == false ? dateTime.Cast(ctx, split[1], tracker) : NullValue.Instance;
                return new DateRangeValue(begin, end);
            }
            else
            {
                return new DateRangeValue(dateTime.Cast(ctx, s, tracker), NullValue.Instance);
            }
        }
    }

    public sealed record ArrayDataType(string Root, string Naked, bool Nullable, DataType Element) : DataType(DataKind.Array, Root, Naked, Nullable)
    {
        private static readonly Regex _split = new Regex(@"[&|\n](?![^()]*\))", RegexOptions.Compiled);
        private static readonly Regex _splitWithComma = new Regex(@"[,&\n](?![^()]*\))", RegexOptions.Compiled);

        protected override DataValue Null() => new ArrayValue([]);

        // point/size/area use commas between their fields, so their elements are split by &, | or newline only.
        protected override DataValue From(Context ctx, object raw, IExcelFileTrackable tracker)
        {
            var split = Element.Kind is DataKind.Point or DataKind.Size or DataKind.Area ? _split : _splitWithComma;
            return new ArrayValue(split.Split($"{raw}")
                .Select(x => x.Trim())
                .Where(x => string.IsNullOrEmpty(x) == false)
                .Select(x => Element.Cast(ctx, x, tracker))
                .ToList());
        }
    }

    public sealed record MapDataType(string Root, string Naked, bool Nullable, DataType Key, DataType Value) : DataType(DataKind.Map, Root, Naked, Nullable)
    {
        private static readonly Regex _split = new Regex(@"[&|\n](?![^()]*\))", RegexOptions.Compiled);

        protected override DataValue Null() => new MapValue([]);

        protected override DataValue From(Context ctx, object raw, IExcelFileTrackable tracker)
        {
            var entries = new List<MapEntry>();
            foreach (var pair in _split.Split($"{raw}").Select(x => x.Trim()).Where(x => string.IsNullOrEmpty(x) == false).Select(x => x.Split(":")))
            {
                if (pair.Length != 2)
                    throw new LogicException($"맵 데이터 포맷이 올바르지 않습니다. ({string.Join(", ", pair)})", tracker);

                var key = Key.Cast(ctx, pair[0].Trim(), tracker);
                if (entries.Any(x => x.Key == key))
                    throw new LogicException($"맵 데이터에 키 {key}가 중복되었습니다.", tracker);

                entries.Add(new MapEntry(key, Value.Cast(ctx, pair[1].Trim(), tracker)));
            }

            return new MapValue(entries);
        }
    }

    // point, size, range or area of Element.
    public sealed record GeometryDataType(DataKind Kind, string Root, string Naked, bool Nullable, DataType Element) : DataType(Kind, Root, Naked, Nullable)
    {
        private static readonly Regex _point = new Regex(@"(?<x>\d+)\s*,\s*(?<y>\d+)", RegexOptions.Compiled);
        private static readonly Regex _size = new Regex(@"(?<width>\d+)\s*,\s*(?<height>\d+)", RegexOptions.Compiled);
        private static readonly Regex _range = new Regex(@"(?<min>\d+)\s*~\s*(?<max>\d+)", RegexOptions.Compiled);
        private static readonly Regex _area = new Regex(@"(?<left>\d+)\s*,\s*(?<top>\d+)\s*,\s*(?<right>\d+)\s*,\s*(?<bottom>\d+)", RegexOptions.Compiled);

        protected override DataValue From(Context ctx, object raw, IExcelFileTrackable tracker)
        {
            var regex = Kind switch
            {
                DataKind.Point => _point,
                DataKind.Size => _size,
                DataKind.Range => _range,
                _ => _area,
            };
            var match = raw is string s ? regex.Match(s) : Match.Empty;
            if (match.Success == false)
                throw new TypeCastException(raw, Root);

            ulong Field(string name) => ((UnsignedValue)Element.Cast(ctx, match.Groups[name].Value, tracker)).Value;
            return Kind switch
            {
                DataKind.Point => new PointValue(Field("x"), Field("y")),
                DataKind.Size => new SizeValue(Field("width"), Field("height")),
                DataKind.Range => new RangeValue(Field("min"), Field("max")),
                _ => new AreaValue(Field("left"), Field("top"), Field("right"), Field("bottom")),
            };
        }
    }

    // Naked is the enum name.
    public sealed record EnumDataType(string Root, string Naked, bool Nullable) : DataType(DataKind.Enum, Root, Naked, Nullable)
    {
        protected override DataValue From(Context ctx, object raw, IExcelFileTrackable tracker)
        {
            if (ctx.Completed.Enum.TryGetValue(Naked, out var members) == false)
                throw new LogicException($"{Naked}는 정의된 열거형 타입이 아닙니다.", tracker);

            var expression = EnumExpression.Parse($"{raw}", false, tracker);
            foreach (var name in expression.Names)
            {
                if (members.ContainsKey(name) == false)
                    throw new LogicException($"{name}는 {Naked}에 존재하지 않는 열거형 데이터입니다.", tracker);
            }

            if (expression.Items.Count == 1 && expression.Items[0].Token != null)
                return new EnumValue(expression.Items[0].Token, null);
            else
                return new EnumValue(null, expression.Evaluate(name => ctx.Completed.Enum.Number(Naked, name), Naked, tracker));
        }
    }
}
