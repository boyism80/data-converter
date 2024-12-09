using ExcelTableConverter.Model;
using ExcelTableConverter.Util;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Range = ExcelTableConverter.Model.Range;

namespace ExcelTableConverter.Factory
{
    public class CastValueFactory : DataFormatFactory<object>
    {
        private readonly Regex _splitRgx = new Regex(@"[&|\n](?![^()]*\))", RegexOptions.Compiled);
        private readonly Regex _pointRgx = new Regex(@"(?<x>\d+)\s*,\s*(?<y>\d+)", RegexOptions.Compiled);
        private readonly Regex _sizeRgx = new Regex(@"(?<width>\d+)\s*,\s*(?<height>\d+)", RegexOptions.Compiled);
        private readonly Regex _rangeRgx = new Regex(@"(?<min>\d+)\s*~\s*(?<max>\d+)", RegexOptions.Compiled);
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<object, object>> _castValuesDP = new ConcurrentDictionary<string, ConcurrentDictionary<object, object>>();

        public CastValueFactory(Context ctx) : base(ctx)
        {

        }

        private object DP(string type, object value, object result)
        {
            value ??= "null";

            var castedValues = _castValuesDP.GetOrAdd(type, _ => new ConcurrentDictionary<object, object>());
            return castedValues.GetOrAdd(value, _ => result);
        }

        private object LazyDP(string type, object value, Func<object> fn)
        {
            value ??= "null";

            var castedValues = _castValuesDP.GetOrAdd(type, _ => new ConcurrentDictionary<object, object>());
            return castedValues.GetOrAdd(value, _ => fn());
        }

        protected override object BooleanType(object value, string root, bool nullable, DataFormatOption option)
        {
            if (value is bool b)
                return b;

            if (Util.Value.IsNull(value))
            {
                if (nullable == false)
                    throw new NullValueException(root);

                return DP(root, value, null);
            }

            if (bool.TryParse($"{value}".ToLower(), out var result) == false)
                throw new TypeCastException(value, root);

            return DP(root, value, result);
        }

        protected override object ArrayType(object value, string root, string e, DataFormatOption option)
        {
            if (value is List<object>)
                return value;

            if (Util.Value.IsNull(value))
                return DP(root, value, new List<object>());

            return DP(root, value, _splitRgx.Split($"{value}")
                .Select(x => x.Trim())
                .Where(x => string.IsNullOrEmpty(x) == false)
                .Select(x => Build(e, x))
                .ToList());
        }

        protected override object DictionaryType(object value, string root, string k, string v, DataFormatOption option)
        {
            if (value is Dictionary<object, object>)
                return value;

            if (Util.Value.IsNull(value))
                return DP(root, value, new Dictionary<object, object>());

            var result = new Dictionary<object, object>();
            foreach (var kvpair in _splitRgx.Split($"{value}").Select(x => x.Trim()).Where(x => string.IsNullOrEmpty(x) == false).Select(x => x.Trim().Split(":")))
            {
                if (kvpair.Length != 2)
                    throw new LogicException($"맵 데이터 포맷이 올바르지 않습니다. ({string.Join(", ", kvpair)})");

                result.Add(Build(k, kvpair[0].Trim()), Build(v, kvpair[1].Trim()));
            }

            return DP(root, value, result);
        }

        protected override object DoubleType(object value, string root, bool nullable, DataFormatOption option)
        {
            if (Util.Value.IsNull(value))
            {
                if (nullable == false)
                    throw new NullValueException(root);

                return DP(root, value, null);
            }

            switch (value)
            {
                case double d:
                    return DP(root, value, d);

                case long l:
                    return DP(root, value, (double)l);

                case int i:
                    return DP(root, value, (double)i);

                default:
                    if (double.TryParse($"{value}", out var result) == false)
                        throw new TypeCastException(value, root);

                    return DP(root, value, result);
            }
        }

        protected override object DslType(object value, string root, bool nullable, DataFormatOption option)
        {
            if (Util.Value.IsNull(value))
            {
                if (nullable == false)
                    throw new NullValueException(root);

                return DP(root, value, null);
            }

            if (Util.Value.IsDSL(value, out var header, out var parameters) == false)
                throw new LogicException($"{value}는 DSL로 변환할 수 없습니다.");

            if (Context.DSL.TryGetValue(header, out var dslRaw) == false)
                throw new LogicException($"{header}는 정의되지 않은 dsl입니다.");

            var dsl = dslRaw as JArray;
            var definedParams = dsl.Select((x, i) => new KeyValuePair<int, JObject>(i, x as JObject)).ToDictionary(x => x.Key, x => x.Value);

            var essentialGroupParams = definedParams.GroupBy(x => x.Value.ContainsKey("default") == false).ToDictionary(x => x.Key, x => x.ToDictionary(x => x.Key, x => x.Value));
            if (essentialGroupParams.TryGetValue(true, out var essentialParams) == false)
                essentialParams = new Dictionary<int, JObject>();

            if (parameters.Count < essentialParams.Count)
                throw new LogicException($"{value} 형식이 올바르지 않습니다. {header}는 최소 {essentialParams.Count}의 인자가 필요합니다. ");

            if (parameters.Count > definedParams.Count)
                throw new LogicException($"{value} 형식이 올바르지 않습니다. {header}는 최대 {definedParams.Count}의 인자만 받습니다.");

            var castedParams = new List<object>();
            for (int i = 0; i < definedParams.Count; i++)
            {
                var param = parameters.ElementAtOrDefault(i);
                if (param == null)
                {
                    if (definedParams[i].TryGetValue("default", out var defaultValue) == false)
                        throw new LogicException($"{header}의 {i + 1}번째 파라미터 {definedParams[i]["name"]}은 디폴트로 정의할 수 없습니다.");

                    param = defaultValue.Value<string>();
                }

                castedParams.Add(Build(definedParams[i]["type"].Value<string>(), param));
            }

            return DP(root, value, new DSL
            {
                Type = header,
                Parameters = castedParams
            });
        }

        private int EnumValueToInt(string root, object value)
        {
            if (value is int i)
                return i;

            var s = value as string;
            if (Context.Result.Enum[root].TryGetValue(s, out var x))
            {
                if (x.Count != 1)
                    throw new LogicException("...?");

                s = x[0] as string;
            }

            if (s.StartsWith("0x"))
                return Convert.ToInt32(s, 16);

            return int.Parse(s);
        }

        private List<object> ToPostfix(string root, List<object> values)
        {
            var data = new Stack<object>();
            var op = new Stack<object>();

            foreach (var value in values)
            {
                switch (value)
                {
                    case List<object> arr:
                        {
                            foreach (var x in ToPostfix(root, arr))
                                data.Push(x);
                        }
                        break;

                    case string s:
                        {
                            switch (s)
                            {
                                case "&":
                                case "|":
                                    if(op.Count > 0)
                                        data.Push(op.Pop());
                                    op.Push(value);
                                    break;

                                default:
                                    data.Push(value);
                                    break;
                            }
                        }
                        break;
                }
            }

            while (op.Count > 0)
            {
                data.Push(op.Pop());
            }
            return data.Reverse().ToList();
        }

        private object GetEnumValue(string root, List<object> values)
        {
            if (values.Count == 1)
                return values[0] as string;

            var stack = new Stack<object>();
            foreach (var x in ToPostfix(root, values))
            {
                switch (x as string)
                {
                    case "&":
                        {
                            var x1 = EnumValueToInt(root, stack.Pop());
                            var x2 = EnumValueToInt(root, stack.Pop());
                            stack.Push(x1 & x2);
                        }
                        break;

                    case "|":
                        {
                            var x1 = EnumValueToInt(root, stack.Pop());
                            var x2 = EnumValueToInt(root, stack.Pop());
                            stack.Push(x1 | x2);
                        }
                        break;

                    default:
                        stack.Push(x as string);
                        break;
                }
            }

            return stack.Pop();
        }

        protected override object EnumType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            if (Util.Value.IsNull(value))
            {
                if (nullable == false)
                    throw new NullValueException(root);

                return null;
            }

            var naked = Util.Type.Nake(root);
            if (Context.Result.Enum.TryGetValue(naked, out var enumSet) == false)
                throw new LogicException($"{naked}는 정의된 열거형 타입이 아닙니다.");

            var parsed = (value as string).ParseValue(false);
            foreach (var x in parsed.ExtractEnumValues())
            {
                if (enumSet.ContainsKey(x) == false)
                    throw new LogicException($"{x}는 {naked}에 존재하지 않는 열거형 데이터입니다.");
            }

            return DP(root, value, GetEnumValue(root, parsed));
        }

        protected override object FloatType(object value, string root, bool nullable, DataFormatOption option)
        {
            var casted = DoubleType(value, root, nullable, option);
            if (casted != null)
            {
                if ((double)casted < float.MinValue)
                    throw new LogicException($"{casted}는 {root} 타입의 최소값보다 작은 값입니다.");

                if ((double)casted > float.MaxValue)
                    throw new LogicException($"{casted}는 {root} 타입의 최대값보다 큰 값입니다.");
            }

            return casted;
        }

        protected override object IntType(object value, string root, bool nullable, DataFormatOption option)
        {
            var casted = LongType(value, root, nullable, option);
            if (casted != null)
            {
                if ((long)casted < int.MinValue)
                    throw new LogicException($"{casted}는 {root} 타입의 최소값보다 작은 값입니다.");

                if ((long)casted > int.MaxValue)
                    throw new LogicException($"{casted}는 {root} 타입의 최대값보다 큰 값입니다.");
            }

            return casted;
        }

        protected override object LongType(object value, string root, bool nullable, DataFormatOption option)
        {
            if (Util.Value.IsNull(value))
            {
                if (nullable == false)
                    throw new NullValueException(root);

                return DP(root, value, null);
            }

            switch (value)
            {
                case long v:
                    return DP(root, value, v);

                case ulong v:
                    if (v > long.MaxValue)
                        throw new LogicException($"{v}는 {root} 타입의 최대값보다 큰 값입니다.");

                    return DP(root, value, v);

                case uint v:
                    return DP(root, value, (long)v);

                case ushort v:
                    return DP(root, value, (long)v);

                case byte v:
                    return DP(root, value, (long)v);

                case float v:
                    return DP(root, value, (long)v);

                case double v:
                    return DP(root, value, (long)v);

                case sbyte v:
                    return DP(root, value, (long)v);

                case short v:
                    return DP(root, value, (long)v);

                case int v:
                    return DP(root, value, (long)v);

                case string v:
                    return DP(root, value, Build(root, v.StartsWith("0x") ? Convert.ToInt64(v, 16) : long.Parse(v)));

                default:
                    if (long.TryParse($"{value}", out var result) == false)
                        throw new TypeCastException(value, root);

                    return DP(root, value, result);
            }
        }

        protected override object ByteType(object value, string root, bool nullable, DataFormatOption option)
        {
            var casted = UlongType(value, root, nullable, option);
            if (casted != null)
            {
                if ((ulong)casted < byte.MinValue)
                    throw new LogicException($"{casted}는 {root} 타입의 최소값보다 작은 값입니다.");

                if ((ulong)casted > byte.MaxValue)
                    throw new LogicException($"{casted}는 {root} 타입의 최대값보다 큰 값입니다.");
            }

            return casted;
        }

        protected override object SbyteType(object value, string root, bool nullable, DataFormatOption option)
        {
            var casted = LongType(value, root, nullable, option);
            if (casted != null)
            {
                if ((long)casted < sbyte.MinValue)
                    throw new LogicException($"{casted}는 {root} 타입의 최소값보다 작은 값입니다.");

                if ((long)casted > sbyte.MaxValue)
                    throw new LogicException($"{casted}는 {root} 타입의 최대값보다 큰 값입니다.");
            }

            return casted;
        }

        protected override object ShortType(object value, string root, bool nullable, DataFormatOption option)
        {
            var casted = LongType(value, root, nullable, option);
            if (casted != null)
            {
                if ((long)casted < short.MinValue)
                    throw new LogicException($"{casted}는 {root} 타입의 최소값보다 작은 값입니다.");

                if ((long)casted > short.MaxValue)
                    throw new LogicException($"{casted}는 {root} 타입의 최대값보다 큰 값입니다.");
            }

            return casted;
        }

        protected override object UshortType(object value, string root, bool nullable, DataFormatOption option)
        {
            var casted = UlongType(value, root, nullable, option);
            if (casted != null)
            {
                if ((ulong)casted < ushort.MinValue)
                    throw new LogicException($"{casted}는 {root} 타입의 최소값보다 작은 값입니다.");

                if ((ulong)casted > ushort.MaxValue)
                    throw new LogicException($"{casted}는 {root} 타입의 최대값보다 큰 값입니다.");
            }

            return casted;
        }

        protected override object UintType(object value, string root, bool nullable, DataFormatOption option)
        {
            var casted = UlongType(value, root, nullable, option);
            if (casted != null)
            {
                if ((ulong)casted < uint.MinValue)
                    throw new LogicException($"{casted}는 {root} 타입의 최소값보다 작은 값입니다.");

                if ((ulong)casted > uint.MaxValue)
                    throw new LogicException($"{casted}는 {root} 타입의 최대값보다 큰 값입니다.");
            }

            return casted;
        }

        protected override object UlongType(object value, string root, bool nullable, DataFormatOption option)
        {
            if (Util.Value.IsNull(value))
            {
                if (nullable == false)
                    throw new NullValueException(root);

                return DP(root, value, null);
            }

            switch (value)
            {
                case ulong v:
                    return DP(root, value, v);

                case uint v:
                    return DP(root, value, (ulong)v);

                case ushort v:
                    return DP(root, value, (ulong)v);

                case byte v:
                    return DP(root, value, (ulong)v);

                case float v:
                    if (v < 0)
                        throw new LogicException($"{v}는 {root} 타입의 최소값보다 작은 값입니다.");

                    return DP(root, value, (ulong)v);

                case double v:
                    if (v < 0)
                        throw new LogicException($"{v}는 {root} 타입의 최소값보다 작은 값입니다.");

                    return DP(root, value, (ulong)v);

                case long v:
                    if (v < 0)
                        throw new LogicException($"{v}는 {root} 타입의 최소값보다 작은 값입니다.");

                    return DP(root, value, v);

                case sbyte v:
                    if (v < 0)
                        throw new LogicException($"{v}는 {root} 타입의 최소값보다 작은 값입니다.");
                    return DP(root, value, (ulong)v);

                case short v:
                    if (v < 0)
                        throw new LogicException($"{v}는 {root} 타입의 최소값보다 작은 값입니다.");
                    return DP(root, value, (ulong)v);

                case int v:
                    if (v < 0)
                        throw new LogicException($"{v}는 {root} 타입의 최소값보다 작은 값입니다.");

                    return DP(root, value, (ulong)v);

                case string v:
                    return DP(root, value, Build(root, v.StartsWith("0x") ? Convert.ToUInt64(v, 16) : ulong.Parse(v)));

                default:
                    if (ulong.TryParse($"{value}", out var result) == false)
                        throw new TypeCastException(value, root);

                    return DP(root, value, result);
            }
        }

        protected override object StringType(object value, string root, DataFormatOption option)
        {
            if (Util.Value.IsNull(value))
            {
                return null;
            }

            if (value is string s)
                return s;

            return DP(root, value, $"{value}");
        }

        protected override object TimeSpanType(object value, string root, bool nullable, DataFormatOption option)
        {
            if (Util.Value.IsNull(value))
            {
                if (nullable == false)
                    throw new NullValueException(root);

                return DP(root, value, null);
            }

            switch (value)
            {
                case TimeSpan ts:
                    return DP(root, value, ts);

                case string s:
                    {
                        if (TimeSpan.TryParse(s.Replace(' ', '.'), out var result) == false)
                            throw new TypeCastException(s, root);

                        return DP(root, value, result);
                    }

                default:
                    throw new TypeCastException(value, root);
            }
        }

        protected override object DateRangeType(object value, string root, bool nullable, DataFormatOption option)
        {
            if (Util.Value.IsNull(value))
            {
                if (nullable == false)
                    throw new NullValueException(root);

                return DP(root, value, null);
            }

            if (value is string s)
            {
                if (s.Contains("~"))
                {
                    var split = s.Split('~', StringSplitOptions.TrimEntries);
                    return DP(root, value, new DateRange
                    {
                        Start = (DateTime)Build("DateTime", split[0]),
                        End = (DateTime)Build("DateTime", split[1])
                    });
                }
                else
                {
                    var ts = (TimeSpan)Build("TimeSpan", s);
                    return DP(root, value, new DateRange
                    {
                        Start = DateTime.MinValue,
                        End = DateTime.MinValue + ts
                    });
                }
            }

            throw new TypeCastException(value, root);
        }

        protected override object DateTimeType(object value, string root, bool nullable, DataFormatOption option)
        {
            if (Util.Value.IsNull(value))
            {
                if (nullable == false)
                    throw new NullValueException(root);

                return DP(root, value, null);
            }

            switch (value)
            {
                case DateTime dt:
                    return DP(root, value, dt);

                case string s:
                    {
                        if (DateTime.TryParse(s, out var result) == false)
                            throw new TypeCastException(s, root);

                        return DP(root, value, result);
                    }

                default:
                    throw new TypeCastException(value, root);
            }
        }

        protected override object PointType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            if (Util.Value.IsNull(value))
            {
                if (nullable == false)
                    throw new NullValueException(root);

                return DP(root, value, null);
            }

            switch (value)
            {
                case Point:
                    return value;

                case string s:
                    return LazyDP(root, value, () =>
                    {
                        var match = _pointRgx.Match(s);
                        if (match.Success == false)
                            throw new TypeCastException(value, root);

                        var x = (ulong)Build(e, match.Groups["x"].Value);
                        var y = (ulong)Build(e, match.Groups["y"].Value);
                        return new Point { X = x, Y = y };
                    });

                default:
                    throw new NotImplementedException();
            }
        }

        protected override object SizeType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            if (Util.Value.IsNull(value))
            {
                if (nullable == false)
                    throw new NullValueException(root);

                return DP(root, value, null);
            }

            switch (value)
            {
                case Size:
                    return value;

                case string s:
                    return LazyDP(root, value, () =>
                    {
                        var match = _sizeRgx.Match(s);
                        if (match.Success == false)
                            throw new TypeCastException(value, root);

                        var width = (ulong)Build(e, match.Groups["width"].Value);
                        var height = (ulong)Build(e, match.Groups["height"].Value);
                        return new Size { Width = width, Height = height };
                    });

                default:
                    throw new NotImplementedException();
            }
        }

        protected override object RangeType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            if (Util.Value.IsNull(value))
            {
                if (nullable == false)
                    throw new NullValueException(root);

                return DP(root, value, null);
            }

            switch (value)
            {
                case Range:
                    return value;

                case string s:
                    return LazyDP(root, value, () =>
                    {
                        var match = _rangeRgx.Match(s);
                        if (match.Success == false)
                            throw new TypeCastException(value, root);

                        var min = (ulong)Build(e, match.Groups["min"].Value);
                        var max = (ulong)Build(e, match.Groups["max"].Value);
                        return new Range { Min = min, Max = max };
                    });

                default:
                    throw new NotImplementedException();
            }
        }

        public object Build(string type, object value)
        {
            return base.Build(type, value);
        }
    }
}
