using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory.Node
{
    public class InitValueFactory : DataFormatFactory<string>
    {
        public InitValueFactory(Context ctx) : base(ctx)
        {
        }

        protected override string ArrayType(object value, string root, string e, DataFormatOption option)
        {
            return $"{new TypeBuilderFactory(Context).Build(root)}(v.{value})";
        }

        protected override string BooleanType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"{new TypeBuilderFactory(Context).Build(root)}(v.{value})";
        }

        protected override string DateRangeType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"{new TypeBuilderFactory(Context).Build(root)}(v.{value})";
        }

        protected override string DateTimeType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"{new TypeBuilderFactory(Context).Build(root)}(v.{value})";
        }

        protected override string DictionaryType(object value, string root, string k, string v, DataFormatOption option)
        {
            return $"{new TypeBuilderFactory(Context).Build(root)}(v.{value})";
        }

        protected override string DoubleType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"{new TypeBuilderFactory(Context).Build(root)}(v.{value})";
        }

        protected override string DslType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"{new TypeBuilderFactory(Context).Build(root)}(v.{value})";
        }

        protected override string EnumType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            return $"{new TypeBuilderFactory(Context).Build(root)}(v.{value})";
        }

        protected override string FloatType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"{new TypeBuilderFactory(Context).Build(root)}(v.{value})";
        }

        protected override string IntType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"{new TypeBuilderFactory(Context).Build(root)}(v.{value})";
        }

        protected override string LongType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"{new TypeBuilderFactory(Context).Build(root)}(v.{value})";
        }

        protected override string ByteType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"{new TypeBuilderFactory(Context).Build(root)}(v.{value})";
        }

        protected override string SbyteType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"{new TypeBuilderFactory(Context).Build(root)}(v.{value})";
        }

        protected override string ShortType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"{new TypeBuilderFactory(Context).Build(root)}(v.{value})";
        }

        protected override string UshortType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"{new TypeBuilderFactory(Context).Build(root)}(v.{value})";
        }

        protected override string UintType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"{new TypeBuilderFactory(Context).Build(root)}(v.{value})";
        }

        protected override string UlongType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"{new TypeBuilderFactory(Context).Build(root)}(v.{value})";
        }

        protected override string StringType(object value, string root, DataFormatOption option)
        {
            return $"{new TypeBuilderFactory(Context).Build(root)}(v.{value})";
        }

        protected override string TimeSpanType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"{new TypeBuilderFactory(Context).Build(root)}(v.{value})";
        }

        protected override string PointType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            return $"{new TypeBuilderFactory(Context).Build(root)}(v.{value})";
        }

        protected override string SizeType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            return $"{new TypeBuilderFactory(Context).Build(root)}(v.{value})";
        }

        protected override string RangeType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            return $"{new TypeBuilderFactory(Context).Build(root)}(v.{value})";
        }

        public string Build(string type, string name)
        {
            return base.Build(type, name);
        }
    }
}
