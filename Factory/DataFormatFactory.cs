using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory
{
    public class DataFormatOption
    {
        private readonly Dictionary<string, object> _options = new Dictionary<string, object>();

        public DataFormatOption()
        { }

        public void Add<T>(string key, T value)
        {
            _options.Add(key, value);
        }

        public T Get<T>(string key)
        {
            return (T)_options.GetValueOrDefault(key);
        }
    }

    public abstract class DataFormatFactory<T>
    {
        protected Context Context { get; private set; }

        protected DataFormatFactory(Context ctx)
        {
            Context = ctx;
        }

        protected virtual bool OnStart(object value, string root, bool nullable, out T result, DataFormatOption option)
        {
            result = default(T);
            return true;
        }

        protected abstract T ByteType(object value, string root, bool nullable, DataFormatOption option);
        protected abstract T SbyteType(object value, string root, bool nullable, DataFormatOption option);
        protected abstract T ShortType(object value, string root, bool nullable, DataFormatOption option);
        protected abstract T UshortType(object value, string root, bool nullable, DataFormatOption option);
        protected abstract T BooleanType(object value, string root, bool nullable, DataFormatOption option);
        protected abstract T IntType(object value, string root, bool nullable, DataFormatOption option);
        protected abstract T UintType(object value, string root, bool nullable, DataFormatOption option);
        protected abstract T LongType(object value, string root, bool nullable, DataFormatOption option);
        protected abstract T UlongType(object value, string root, bool nullable, DataFormatOption option);
        protected abstract T DoubleType(object value, string root, bool nullable, DataFormatOption option);
        protected abstract T FloatType(object value, string root, bool nullable, DataFormatOption option);
        protected abstract T StringType(object value, string root, DataFormatOption option);
        protected abstract T DictionaryType(object value, string root, string k, string v, DataFormatOption option);
        protected abstract T ArrayType(object value, string root, string e, DataFormatOption option);
        protected abstract T EnumType(object value, string root, string e, bool nullable, DataFormatOption option);
        protected abstract T DslType(object value, string root, bool nullable, DataFormatOption option);
        protected abstract T TimeSpanType(object value, string root, bool nullable, DataFormatOption option);
        protected abstract T DateTimeType(object value, string root, bool nullable, DataFormatOption option);
        protected abstract T DateRangeType(object value, string root, bool nullable, DataFormatOption option);
        protected abstract T PointType(object value, string root, string e, bool nullable, DataFormatOption option);
        protected abstract T SizeType(object value, string root, string e, bool nullable, DataFormatOption option);
        protected abstract T RangeType(object value, string root, string e, bool nullable, DataFormatOption option);

        protected T Build(string type, object value, DataFormatOption option = null)
        {
            option ??= new DataFormatOption();

            if (Util.Value.IsConst(value, out var constTableName, out var constValueName))
            {
                var sorted = Context.RawConst.SelectMany(x => x.Value)
                    .GroupBy(x => x.TableName)
                    .ToDictionary(x => x.Key, x => x.ToDictionary(x => x.Name));

                if (sorted.TryGetValue(constTableName, out var constSet) == false)
                    throw new LogicException($"{constTableName}은 상수 테이블에 정의되지 않았습니다.");

                if (constSet.TryGetValue(constValueName, out var constValue) == false)
                    throw new LogicException($"{constValueName}은 {constTableName}에 정의되지 않았습니다.");

                return Build(constValue.Type, constValue.Value);
            }

            var root = Context.GetRootTableType(type);
            var naked = Util.Type.Nake(root);
            var nullable = Util.Type.IsNullable(root);

            if (Util.Type.IsSequence(type, out _))
            {
                if (nullable)
                    return Build("int?", value, option);
                else
                    return Build("int", value, option);
            }

            if (OnStart(value, root, nullable, out var result, option) == false)
            {
                return result;
            }

            switch (naked)
            {
                case "byte":
                case "uint8":
                case "uint8_t":
                    return ByteType(value, root, nullable, option);

                case "sbyte":
                case "int8":
                case "int8_t":
                    return SbyteType(value, root, nullable, option);

                case "short":
                case "int16":
                case "int16_t":
                    return ShortType(value, root, nullable, option);

                case "ushort":
                case "uint16":
                case "uint16_t":
                    return UshortType(value, root, nullable, option);

                case "bool":
                    return BooleanType(value, root, nullable, option);

                case "int":
                case "int32":
                case "int32_t":
                    return IntType(value, root, nullable, option);

                case "uint":
                case "uint32":
                case "uint32_t":
                    return UintType(value, root, nullable, option);

                case "long":
                case "int64":
                case "int64_t":
                    return LongType(value, root, nullable, option);

                case "ulong":
                case "uint64":
                case "uint64_t":
                    return UlongType(value, root, nullable, option);

                case "double":
                    return DoubleType(value, root, nullable, option);

                case "float":
                    return FloatType(value, root, nullable, option);

                case "string":
                    return StringType(value, root, option);

                case "dsl":
                    return DslType(value, root, nullable, option);

                case "TimeSpan":
                    return TimeSpanType(value, root, nullable, option);

                case "DateTime":
                    return DateTimeType(value, root, nullable, option);

                case "DateRange":
                    return DateRangeType(value, root, nullable, option);
            }

            if (Util.Type.IsArray(naked, out var e))
            {
                return ArrayType(value, root, e, option);
            }

            if (Util.Type.IsMap(naked, out var pair))
            {
                return DictionaryType(value, root, pair.Key, pair.Value, option);
            }

            if (Util.Type.IsPoint(naked, out var pointType))
            {
                return PointType(value, root, string.IsNullOrEmpty(pointType) ? "uint" : pointType, nullable, option);
            }

            if (Util.Type.IsSize(naked, out var sizeType))
            {
                return SizeType(value, root, string.IsNullOrEmpty(sizeType) ? "uint" : sizeType, nullable, option);
            }

            if (Util.Type.IsRange(naked, out var rangeType))
            {
                return RangeType(value, root, string.IsNullOrEmpty(rangeType) ? "uint" : rangeType, nullable, option);
            }

            if (Context.Result.Enum.ContainsKey(naked))
            {
                return EnumType(value, root, naked, nullable, option);
            }

            throw new NotImplementedException($"{naked} 타입은 정의되지 않은 타입 형식입니다.");
        }
    }
}
