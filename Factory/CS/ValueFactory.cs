using ExcelTableConverter.Model;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace ExcelTableConverter.Factory.CS
{
    public class ValueFactory : DataFormatFactory<object>
    {
        private readonly Regex _splitRgx = new Regex(@"[&|\n](?![^()]*\))", RegexOptions.Compiled);
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<object, object>> _castValuesDP = new ConcurrentDictionary<string, ConcurrentDictionary<object, object>>();

        public ValueFactory(Context ctx) : base(ctx)
        {

        }

        private object DP(string type, object value, object result)
        {
            value ??= "null";

            var castedValues = _castValuesDP.GetOrAdd(type, _ => new ConcurrentDictionary<object, object>());
            return castedValues.GetOrAdd(value, _x => result);
        }

        protected override object BooleanType(object value, string root, bool nullable)
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

        protected override object ArrayType(object value, string root, string e)
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

        protected override object DictionaryType(object value, string root, string k, string v)
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

        protected override object DoubleType(object value, string root, bool nullable)
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

        protected override object DslType(object value, string root, bool nullable)
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

        protected override object EnumType(object value, string root, string e, bool nullable)
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

            if (enumSet.ContainsKey(value as string) == false)
                throw new LogicException($"{value}는 {naked}에 존재하지 않는 열거형 데이터입니다.");

            return DP(root, value, $"{value}");
        }

        protected override object FloatType(object value, string root, bool nullable)
        {
            return DoubleType(value, root, nullable);
        }

        protected override object IntType(object value, string root, bool nullable)
        {
            return LongType(value, root, nullable);
        }

        protected override object LongType(object value, string root, bool nullable)
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
                    return DP(root, value, (long)d);

                case long l:
                    return DP(root, value, l);

                case int i:
                    return DP(root, value, (long)i);

                default:
                    if (long.TryParse($"{value}", out var result) == false)
                        throw new TypeCastException(value, root);

                    return DP(root, value, result);
            }
        }

        protected override object StringType(object value, string root)
        {
            if (Util.Value.IsNull(value))
            {
                return null;
            }

            if (value is string s)
                return s;

            return DP(root, value, $"{value}");
        }

        protected override object TimeSpanType(object value, string root, bool nullable)
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

        protected override object DateRangeType(object value, string root, bool nullable)
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

        protected override object DateTimeType(object value, string root, bool nullable)
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

        public new object Build(string type, object value)
        {
            return base.Build(type, value);
        }
    }
}
