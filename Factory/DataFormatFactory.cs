using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory
{
    public abstract class DataFormatFactory<T>
    {
        protected Context Context { get; private set; }

        protected DataFormatFactory(Context ctx)
        {
            Context = ctx;
        }

        protected virtual bool OnStart(object value, string root, bool nullable, out T result)
        {
            result = default(T);
            return true;
        }

        protected abstract T BooleanType(object value, string root, bool nullable);

        protected abstract T IntType(object value, string root, bool nullable);

        protected abstract T LongType(object value, string root, bool nullable);

        protected abstract T DoubleType(object value, string root, bool nullable);

        protected abstract T FloatType(object value, string root, bool nullable);

        protected abstract T StringType(object value, string root);

        protected abstract T DictionaryType(object value, string root, string k, string v);

        protected abstract T ArrayType(object value, string root, string e);

        protected abstract T EnumType(object value, string root, string e, bool nullable);

        protected abstract T DslType(object value, string root, bool nullable);

        protected abstract T TimeSpanType(object value, string root, bool nullable);

        protected abstract T DateTimeType(object value, string root, bool nullable);

        protected abstract T DateRangeType(object value, string root, bool nullable);

        protected T Build(string type, object value)
        {
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
                    return Build("int?", value);
                else
                    return Build("int", value);
            }

            if (OnStart(value, root, nullable, out var result) == false)
            {
                return result;
            }

            switch (naked)
            {
                case "bool":
                    return BooleanType(value, root, nullable);

                case "int":
                    return IntType(value, root, nullable);

                case "long":
                    return LongType(value, root, nullable);

                case "double":
                    return DoubleType(value, root, nullable);

                case "float":
                    return FloatType(value, root, nullable);

                case "string":
                    return StringType(value, root);

                case "dsl":
                    return DslType(value, root, nullable);

                case "TimeSpan":
                    return TimeSpanType(value, root, nullable);

                case "DateTime":
                    return DateTimeType(value, root, nullable);

                case "DateRange":
                    return DateRangeType(value, root, nullable);
            }

            if (Util.Type.IsArray(naked, out var e))
            {
                return ArrayType(value, root, e);
            }

            if (Util.Type.IsMap(naked, out var pair))
            {
                return DictionaryType(value, root, pair.Key, pair.Value);
            }

            if (Context.Result.Enum.ContainsKey(naked))
            {
                return EnumType(value, root, naked, nullable);
            }

            throw new NotImplementedException($"{naked} 타입은 정의되지 않은 타입 형식입니다.");
        }
    }
}
