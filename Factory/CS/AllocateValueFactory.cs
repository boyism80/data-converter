using ExcelTableConverter.Model;
using Newtonsoft.Json.Linq;

namespace ExcelTableConverter.Factory.CS
{
    public class AllocateValueFactory : DataFormatFactory<string>
    {
        public AllocateValueFactory(Context ctx) : base(ctx)
        {

        }

        protected override bool OnStart(object value, string root, bool nullable, out string result, DataFormatOption option)
        {
            if (Util.Value.IsNull(value))
            {
                result = "null";
                return false;
            }

            result = string.Empty;
            return true;
        }

        protected override string ArrayType(object value, string root, string e, DataFormatOption option)
        {
            var list = value as List<object>;
            var values = string.Join(", ", list.Select(x => Build(e, x)));

            return $"new {new TypeFactory(Context).Build(root)} {{ {values} }}";
        }

        protected override string BooleanType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"{value}";
        }

        protected override string DateRangeType(object value, string root, bool nullable, DataFormatOption option)
        {
            switch (value)
            {
                case DateRange dr:
                    return $"new DateRange {{ Begin = {Build("DateTime", dr.Start)}, End = {Build("DateTime", dr.End)} }}";

                default:
                    throw new InvalidOperationException("알 수 없는 에러");
            }
        }

        protected override string DateTimeType(object value, string root, bool nullable, DataFormatOption option)
        {
            var dt = (DateTime)value;
            return $"DateTime.Parse({Build("string", dt.ToString("yyyy-MM-dd HH:mm:ss"))})";
        }

        protected override string DictionaryType(object value, string root, string k, string v, DataFormatOption option)
        {
            var map = value as Dictionary<object, object>;
            var values = string.Join(", ", map.Select(x =>
            {
                var key = x.Key;
                var keyType = k;
                var value = x.Value;
                var valueType = v;
                return $"[{Build(keyType, key)}] = {Build(valueType, value)}";
            }));
            return $"new Dictionary<{new TypeFactory(Context).Build(k)}, {new TypeFactory(Context).Build(v)}> {{ {values} }}";
        }

        protected override string DoubleType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"{value}";
        }

        protected override string DslType(object value, string root, bool nullable, DataFormatOption option)
        {
            var dsl = value as DSL;

            if (Context.DSL.TryGetValue(dsl.Type, out var prototype) == false)
                throw new LogicException($"{dsl.Type}는 정의되지 않은 DSL 형식입니다.");

            var args = dsl.Parameters.Select((x, i) =>
            {
                var param = (prototype as JArray).ElementAt(i) as JObject;
                var name = param["name"].Value<string>();
                var type = param["type"].Value<string>();
                return (Name: name, Value: Build(type, x));
            }).ToList();

            return $"new MasterData.Types.Dsl.Parameter.{dsl.Type} {{ {string.Join(", ", args.Select(x => $"{x.Name} = {x.Value}"))} }}.ToDsl()";
        }

        protected override string EnumType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            foreach (var (k, v) in Context.Result.Enum[root])
            {
                return $"{root}.{k}";
            }

            throw new LogicException($"{value}는 {root} 열거형에 존재하지 않는 값입니다.");
        }

        protected override string FloatType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"{value}";
        }

        protected override string IntType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"{value}";
        }

        protected override string LongType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"{value}";
        }

        protected override string ByteType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"{value}";
        }

        protected override string SbyteType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"{value}";
        }

        protected override string ShortType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"{value}";
        }

        protected override string UshortType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"{value}";
        }

        protected override string UintType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"{value}";
        }

        protected override string UlongType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"{value}";
        }

        protected override string StringType(object value, string root, DataFormatOption option)
        {
            var s = value as string;
            if (string.IsNullOrEmpty(s))
                return "string.Empty";
            else if (s.Contains('\n'))
                return $"@\"{s}\"";
            else
                return $"\"{s}\"";
        }

        protected override string TimeSpanType(object value, string root, bool nullable, DataFormatOption option)
        {
            var ts = (TimeSpan)value;
            return $"TimeSpan.Parse({Build("string", ts.ToString())})";
        }

        public string Build(string type, object value)
        {
            return base.Build(type, value);
        }

        protected override string PointType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            throw new NotImplementedException();
        }

        protected override string SizeType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            throw new NotImplementedException();
        }

        protected override string RangeType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            throw new NotImplementedException();
        }
    }
}
