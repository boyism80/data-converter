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
            return $"[]{Build(e, null, option)}";
        }

        protected override string BooleanType(object value, string root, bool nullable, DataFormatOption option)
        {
            return "bool";
        }

        protected override string DateRangeType(object value, string root, bool nullable, DataFormatOption option)
        {
            return "DateRange";
        }

        protected override string DateTimeType(object value, string root, bool nullable, DataFormatOption option)
        {
            return "time.Duration";
        }

        protected override string DictionaryType(object value, string root, string k, string v, DataFormatOption option)
        {
            return $"map[{Build(k, null, option)}]{Build(v, null, option)}";
        }

        protected override string FloatType(object value, string root, bool nullable, DataFormatOption option)
        {
            return "float64";
        }

        protected override string DslType(object value, string root, bool nullable, DataFormatOption option)
        {
            return "Dsl";
        }

        protected override string EnumType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            return root;
        }

        protected override string IntType(object value, string root, bool nullable, DataFormatOption option)
        {
            return "int";
        }

        protected override string LongType(object value, string root, bool nullable, DataFormatOption option)
        {
            return "int64";
        }

        protected override string ByteType(object value, string root, bool nullable, DataFormatOption option)
        {
            return "byte";
        }

        protected override string SbyteType(object value, string root, bool nullable, DataFormatOption option)
        {
            return "int8";
        }

        protected override string ShortType(object value, string root, bool nullable, DataFormatOption option)
        {
            return "int16";
        }

        protected override string UshortType(object value, string root, bool nullable, DataFormatOption option)
        {
            return "uint16";
        }

        protected override string UintType(object value, string root, bool nullable, DataFormatOption option)
        {
            return "uint32";
        }

        protected override string UlongType(object value, string root, bool nullable, DataFormatOption option)
        {
            return "uint64";
        }

        protected override string StringType(object value, string root, DataFormatOption option)
        {
            return "string";
        }

        protected override string TimeSpanType(object value, string root, bool nullable, DataFormatOption option)
        {
            return "Duration";
        }

        protected override string PointType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            return $"Point[{Build(e, null, option)}]";
        }

        protected override string SizeType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            return $"Size[{Build(e, null, option)}]";
        }

        protected override string RangeType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            return $"Range[{Build(e, null, option)}]";
        }

        protected override string AreaType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            return "Area";
        }

        protected override string DoubleType(object value, string root, bool nullable, DataFormatOption option)
        {
            return "float64";
        }

        public new string Build(string type)
        {
            return base.Build(type, null, new DataFormatOption());
        }

        public new string Build(string type, object value, DataFormatOption option)
        {
            return base.Build(type, value, option);
        }
    }
}
