using ExcelTableConverter.Model;
using System.Reflection.Emit;

namespace ExcelTableConverter.Factory.CPP
{
    public class ValueDeserializeFactory : DataFormatFactory<string>
    {
        public ValueDeserializeFactory(Context ctx) : base(ctx)
        {

        }

        private static string WithNullable(object obj, string type, bool nullable)
        {
            if (nullable)
                return $"any_cast<std::optional<{type}>>({obj})";
            else
                return $"any_cast<{type}>({obj})";
        }

        protected override string ArrayType(object obj, string root, string e, DataFormatOption option)
        {
            return WithNullable(obj, $"std::vector<{new TypeFactory(Context).Build(e)}>", false);
        }

        protected override string BooleanType(object obj, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable(obj, $"bool", nullable);
        }

        protected override string DateRangeType(object obj, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable(obj, $"fb::model::date_range", nullable);
        }

        protected override string DateTimeType(object obj, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable(obj, $"boost::posix_time::ptime", nullable);
        }

        protected override string DictionaryType(object obj, string root, string k, string v, DataFormatOption option)
        {
            return WithNullable(obj, $"std::map<{new TypeFactory(Context).Build(k)}, {new TypeFactory(Context).Build(v)}>", false);
        }

        protected override string DoubleType(object obj, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable(obj, $"double", nullable);
        }

        protected override string DslType(object obj, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable(obj, $"dsl", false);
        }

        protected override string EnumType(object obj, string root, string e, bool nullable, DataFormatOption option)
        {
            return WithNullable(obj, $"{new TypeFactory(Context).Build(root)}", false);
        }

        protected override string FloatType(object obj, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable(obj, $"float", nullable);
        }

        protected override string IntType(object obj, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable(obj, $"int", nullable);
        }

        protected override string LongType(object obj, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable(obj, $"long", nullable);
        }

        protected override string StringType(object obj, string root, DataFormatOption option)
        {
            return WithNullable(obj, $"std::string", false);
        }

        protected override string TimeSpanType(object obj, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable(obj, $"std::chrono::milliseconds", nullable);
        }

        public string Build(string type, string value)
        {
            return base.Build(type, value);
        }
    }
}
