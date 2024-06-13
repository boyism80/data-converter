using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory.CPP
{
    public class TypeFactory : DataFormatFactory<string>
    {
        public TypeFactory(Context ctx) : base(ctx)
        { }

        private string WithNullable(string type, bool nullable)
        {
            if (nullable)
                return $"std::optional<{Util.Type.Nake(type)}>";
            else
                return type;
        }

        protected override string ArrayType(object value, string root, string e, DataFormatOption option)
        {
            var result = $"std::vector<{Build(e)}>";
            if (option.Get<bool>("rvalue"))
                result = $"const {result}&";
            return result;
        }

        protected override string BooleanType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable(root, nullable);
        }

        protected override string DateRangeType(object value, string root, bool nullable, DataFormatOption option)
        {
            var result = WithNullable($"{Util.CPP.Namespace.Access(Context.Config.Namespace)}date_range", nullable);
            if (option.Get<bool>("rvalue"))
                result = $"const {result}&";
            return result;
        }

        protected override string DateTimeType(object value, string root, bool nullable, DataFormatOption option)
        {
            var result = WithNullable("boost::posix_time::ptime", nullable);
            if (option.Get<bool>("rvalue"))
                result = $"const {result}&";
            return result;
        }

        protected override string DictionaryType(object value, string root, string k, string v, DataFormatOption option)
        {
            var result = $"std::map<{Build(k)}, {Build(v)}>";
            if (option.Get<bool>("rvalue"))
                result = $"const {result}&";

            return result;
        }

        protected override string DoubleType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable(root, nullable);
        }

        protected override string DslType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable($"{Util.CPP.Namespace.Access(Context.Config.Namespace)}dsl", nullable);
        }

        protected override string EnumType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            return WithNullable($"{Util.CPP.Namespace.Access(Context.Config.Namespace)}{Util.Type.Nake(root)}", nullable);
        }

        protected override string FloatType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable(root, nullable);
        }

        protected override string IntType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable(root, nullable);
        }

        protected override string LongType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("long long", nullable);
        }

        protected override string StringType(object value, string root, DataFormatOption option)
        {
            return option.Get<bool>("rvalue") ? "const std::string&" : "std::string";
        }

        protected override string TimeSpanType(object value, string root, bool nullable, DataFormatOption option)
        {
            var result = WithNullable("std::chrono::milliseconds", nullable);
            if (option.Get<bool>("rvalue"))
                result = $"const {result}&";
            return result;
        }

        public string Build(string type, bool rvalue = false)
        {
            var option = new DataFormatOption();
            option.Add("rvalue", rvalue);
            return Build(type, null, option);
        }
    }
}
