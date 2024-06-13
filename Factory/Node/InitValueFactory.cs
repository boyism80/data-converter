using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory.Node
{
    public class InitValueFactory : DataFormatFactory<string>
    {
        public InitValueFactory(Context ctx) : base(ctx)
        {
        }

        private static string WithNullable(string root, object value, bool nullable)
        {
            if (nullable)
                root = $"std::optional<{root}>";

            return $"{Util.CPP.Namespace.Access(Context.Config.Namespace)}build<{root}>(json[\"{value}\"])";
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

        protected override string StringType(object value, string root, DataFormatOption option)
        {
            return $"{new TypeBuilderFactory(Context).Build(root)}(v.{value})";
        }

        protected override string TimeSpanType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"{new TypeBuilderFactory(Context).Build(root)}(v.{value})";
        }

        public string Build(string type, string name)
        {
            return base.Build(type, name);
        }
    }
}
