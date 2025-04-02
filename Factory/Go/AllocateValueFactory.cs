using ExcelTableConverter.Model;
using Newtonsoft.Json.Linq;

namespace ExcelTableConverter.Factory.Go
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
            throw new LogicException($"golang에서는 지원하지 않는 상수 타입입니다. - {root}".AsSpan());
        }

        protected override string BooleanType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"{value}";
        }

        protected override string DateRangeType(object value, string root, bool nullable, DataFormatOption option)
        {
            throw new LogicException($"golang에서는 지원하지 않는 상수 타입입니다. - {root}".AsSpan());
        }

        protected override string DateTimeType(object value, string root, bool nullable, DataFormatOption option)
        {
            throw new LogicException($"golang에서는 지원하지 않는 상수 타입입니다. - {root}".AsSpan());
        }

        protected override string DictionaryType(object value, string root, string k, string v, DataFormatOption option)
        {
            throw new LogicException($"golang에서는 지원하지 않는 상수 타입입니다. - {root}".AsSpan());
        }

        protected override string DoubleType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"{value}";
        }

        protected override string DslType(object value, string root, bool nullable, DataFormatOption option)
        {
            throw new LogicException($"golang에서는 지원하지 않는 상수 타입입니다. - {root}".AsSpan());
        }

        protected override string EnumType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            foreach (var (k, v) in Context.Result.Enum[root])
            {
                return $"{root}.{k}";
            }

            throw new LogicException($"{value}는 {root} 열거형에 존재하지 않는 값입니다.".AsSpan());
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
            throw new LogicException($"golang에서는 지원하지 않는 상수 타입입니다. - {root}".AsSpan());
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

        protected override string AreaType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            throw new NotImplementedException();
        }
    }
}
