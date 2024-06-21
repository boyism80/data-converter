using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory.CPP
{
    public class TypeFactory : DataFormatFactory<string>
    {
        public TypeFactory(Context ctx) : base(ctx)
        { }

        private string WithNullable(string type, bool nullable, DataFormatOption option)
        {
            var amp = option.Get<bool>("amp");
            var result = nullable ?
                $"std::optional<{Util.Type.Nake(type)}>" : type;

            if(amp && type.EndsWith('&') == false)
                result = $"{result}&";

            return result;
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
            return WithNullable(root, nullable, option);
        }

        protected override string DateRangeType(object value, string root, bool nullable, DataFormatOption option)
        {
            var result = WithNullable($"{Util.CPP.Namespace.Access(Context.Config.Namespace)}date_range", nullable, option);
            if (option.Get<bool>("rvalue"))
                result = $"const {result}&";
            return result;
        }

        protected override string DateTimeType(object value, string root, bool nullable, DataFormatOption option)
        {
            var result = WithNullable("boost::posix_time::ptime", nullable, option);
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
            return WithNullable(root, nullable, option);
        }

        protected override string DslType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable($"{Util.CPP.Namespace.Access(Context.Config.Namespace)}dsl", nullable, option);
        }

        protected override string EnumType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            return WithNullable($"{Util.CPP.Namespace.Access(Context.Config.Namespace)}{Util.CPP.Namespace.Access(Context.Config.EnumNamespace)}{Util.Type.Nake(root)}", nullable, option);
        }

        protected override string FloatType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable(root, nullable, option);
        }

        protected override string IntType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable(root, nullable, option);
        }

        protected override string LongType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("int64_t", nullable, option);
        }

        protected override string ByteType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("uint8_t", nullable, option);
        }

        protected override string SbyteType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("int8_t", nullable, option);
        }

        protected override string ShortType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("int16_t", nullable, option);
        }

        protected override string UshortType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("uint16_t", nullable, option);
        }

        protected override string UintType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("uint32_t", nullable, option);
        }

        protected override string UlongType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("uint64_t", nullable, option);
        }

        protected override string StringType(object value, string root, DataFormatOption option)
        {
            if(option.Get<bool>("raw"))
                return WithNullable(option.Get<bool>("rvalue") ? "const char*" : "char*", false, option);
            else
                return WithNullable(option.Get<bool>("rvalue") ? "const std::string&" : "std::string", false, option);
        }

        protected override string TimeSpanType(object value, string root, bool nullable, DataFormatOption option)
        {
            var result = WithNullable("std::chrono::milliseconds", nullable, option);
            if (option.Get<bool>("rvalue"))
                result = $"const {result}&";
            return result;
        }

        protected override string Point8Type(object value, string root, bool nullable, DataFormatOption option)
        {
            var result = WithNullable("point8_t", nullable, option);
            if (option.Get<bool>("rvalue"))
                result = $"const {result}&";
            return result;
        }

        protected override string Point16Type(object value, string root, bool nullable, DataFormatOption option)
        {
            var result = WithNullable("point16_t", nullable, option);
            if (option.Get<bool>("rvalue"))
                result = $"const {result}&";
            return result;
        }

        protected override string Point32Type(object value, string root, bool nullable, DataFormatOption option)
        {
            var result = WithNullable("point32_t", nullable, option);
            if (option.Get<bool>("rvalue"))
                result = $"const {result}&";
            return result;
        }

        protected override string Point64Type(object value, string root, bool nullable, DataFormatOption option)
        {
            var result = WithNullable("point64_t", nullable, option);
            if (option.Get<bool>("rvalue"))
                result = $"const {result}&";
            return result;
        }

        protected override string Size8Type(object value, string root, bool nullable, DataFormatOption option)
        {
            var result = WithNullable("size8_t", nullable, option);
            if (option.Get<bool>("rvalue"))
                result = $"const {result}&";
            return result;
        }

        protected override string Size16Type(object value, string root, bool nullable, DataFormatOption option)
        {
            var result = WithNullable("size16_t", nullable, option);
            if (option.Get<bool>("rvalue"))
                result = $"const {result}&";
            return result;
        }

        protected override string Size32Type(object value, string root, bool nullable, DataFormatOption option)
        {
            var result = WithNullable("size32_t", nullable, option);
            if (option.Get<bool>("rvalue"))
                result = $"const {result}&";
            return result;
        }

        protected override string Size64Type(object value, string root, bool nullable, DataFormatOption option)
        {
            var result = WithNullable("size64_t", nullable, option);
            if (option.Get<bool>("rvalue"))
                result = $"const {result}&";
            return result;
        }

        protected override string Range8Type(object value, string root, bool nullable, DataFormatOption option)
        {
            var result = WithNullable("range8_t", nullable, option);
            if (option.Get<bool>("rvalue"))
                result = $"const {result}&";
            return result;
        }

        protected override string Range16Type(object value, string root, bool nullable, DataFormatOption option)
        {
            var result = WithNullable("range16_t", nullable, option);
            if (option.Get<bool>("rvalue"))
                result = $"const {result}&";
            return result;
        }

        protected override string Range32Type(object value, string root, bool nullable, DataFormatOption option)
        {
            var result = WithNullable("range32_t", nullable, option);
            if (option.Get<bool>("rvalue"))
                result = $"const {result}&";
            return result;
        }

        protected override string Range64Type(object value, string root, bool nullable, DataFormatOption option)
        {
            var result = WithNullable("range64_t", nullable, option);
            if (option.Get<bool>("rvalue"))
                result = $"const {result}&";
            return result;
        }

        public string Build(string type, bool rvalue = false, bool amp = false, bool raw = false)
        {
            var option = new DataFormatOption();
            option.Add("rvalue", rvalue);
            option.Add("amp", amp);
            option.Add("raw", raw);
            return Build(type, null, option);
        }
    }
}
