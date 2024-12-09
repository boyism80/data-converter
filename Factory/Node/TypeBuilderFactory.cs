using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory.Node
{
    public class TypeBuilderFactory : DataFormatFactory<string>
    {
        public TypeBuilderFactory(Context ctx) : base(ctx)
        {
        }

        protected override string ArrayType(object value, string root, string e, DataFormatOption option)
        {
            return $"ArrayBuilder({Build(e)}).build";
        }

        protected override string BooleanType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"DefaultBuilder().build";
        }

        protected override string DateRangeType(object value, string root, bool nullable, DataFormatOption option)
        {
            return "DateRangeBuilder().build";
        }

        protected override string DateTimeType(object value, string root, bool nullable, DataFormatOption option)
        {
            return "DateTimeBuilder().build";
        }

        protected override string DictionaryType(object value, string root, string k, string v, DataFormatOption option)
        {
            return $"DictionaryBuilder({Build(k)}, {Build(v)}).build";
        }

        protected override string DoubleType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"DefaultBuilder().build";
        }

        protected override string DslType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"DslBuilder().build";
        }

        protected override string EnumType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            return $"EnumBuilder(\"{e}\").build";
        }

        protected override string FloatType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"DefaultBuilder().build";
        }

        protected override string IntType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"DefaultBuilder().build";
        }

        protected override string LongType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"DefaultBuilder().build";
        }

        protected override string ByteType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"DefaultBuilder().build";
        }

        protected override string SbyteType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"DefaultBuilder().build";
        }

        protected override string ShortType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"DefaultBuilder().build";
        }

        protected override string UshortType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"DefaultBuilder().build";
        }

        protected override string UintType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"DefaultBuilder().build";
        }

        protected override string UlongType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"DefaultBuilder().build";
        }

        protected override string StringType(object value, string root, DataFormatOption option)
        {
            return $"DefaultBuilder().build";
        }

        protected override string TimeSpanType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"TimeSpanBuilder().build";
        }

        protected override string PointType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            return $"PointBuilder().build";
        }

        protected override string SizeType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            return $"SizeBuilder().build";
        }

        protected override string RangeType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            return $"RangeBuilder().build";
        }

        public string Build(string type)
        {
            return base.Build(type, null);
        }
    }
}
