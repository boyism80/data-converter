using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory.Go
{
    public class TypeFactory : DataFormatFactory<string>
    {
        public TypeFactory(Context ctx) : base(ctx)
        {
        }

        private string WithNullable(string type, bool nullable)
        {
            if (nullable)
                return $"*{type}";
            else
                return type;
        }

        protected override string ArrayType(object value, string root, string e, DataFormatOption option)
        {
            return WithNullable($"[]{Build(e, null, option)}", false);
        }

        protected override string BooleanType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("bool", nullable);
        }

        protected override string DateRangeType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("DateRange", nullable);
        }

        protected override string DateTimeType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("time.Duration", nullable);
        }

        protected override string DictionaryType(object value, string root, string k, string v, DataFormatOption option)
        {
            return WithNullable($"map[{Build(k, null, option)}]{Build(v, null, option)}", false);
        }

        protected override string FloatType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("float32", nullable);
        }

        protected override string DslType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("Dsl", nullable);
        }

        protected override string EnumType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            return WithNullable(root, nullable);
        }

        protected override string IntType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("int", nullable);
        }

        protected override string LongType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("int64", nullable);
        }

        protected override string ByteType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("byte", nullable);
        }

        protected override string SbyteType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("int8", nullable);
        }

        protected override string ShortType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("int16", nullable);
        }

        protected override string UshortType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("uint16", nullable);
        }

        protected override string UintType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("uint32", nullable);
        }

        protected override string UlongType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("uint64", nullable);
        }

        protected override string StringType(object value, string root, DataFormatOption option)
        {
            return WithNullable("string", false);
        }

        protected override string TimeSpanType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("Duration", nullable);
        }

        protected override string PointType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            return WithNullable($"Point[{Build(e, null, option)}]", nullable);
        }

        protected override string SizeType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            return WithNullable($"Size[{Build(e, null, option)}]", nullable);
        }

        protected override string RangeType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            return WithNullable($"Range[{Build(e, null, option)}]", nullable);
        }

        protected override string AreaType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            return WithNullable("Area", nullable);
        }

        protected override string DoubleType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("float64", nullable);
        }

        public string Build(string type)
        {
            return base.Build(type, null, new DataFormatOption());
        }

        public string Build(string type, object value, DataFormatOption option)
        {
            return base.Build(type, value, option);
        }
    }
}
