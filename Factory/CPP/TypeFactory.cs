using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory.CPP
{
    public class TypeFactory : DataFormatFactory<string>
    {
        public TypeFactory(Context ctx) : base(ctx)
        { }

        private string WithNullable(string type, bool nullable, DataFormatOption option)
        {
            var result = nullable ?
                $"std::optional<{Util.Type.Nake(type)}>" : type;

            return result;
        }

        protected override string ArrayType(object value, string root, string e, DataFormatOption option)
        {
            return $"std::vector<{Build(e)}>";
        }

        protected override string BooleanType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable(root, nullable, option);
        }

        protected override string DateRangeType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable($"{Util.CPP.Namespace.Access(Context.Config.Namespace)}date_range", nullable, option);
        }

        protected override string DateTimeType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("boost::posix_time::ptime", nullable, option);
        }

        protected override string DictionaryType(object value, string root, string k, string v, DataFormatOption option)
        {
            return $"std::map<{Build(k)}, {Build(v)}>";
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
            return WithNullable("std::string", false, option);
        }

        protected override string TimeSpanType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("std::chrono::milliseconds", nullable, option);
        }

        protected override string Point8Type(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("point8_t", nullable, option);
        }

        protected override string Point16Type(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("point16_t", nullable, option);
        }

        protected override string Point32Type(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("point32_t", nullable, option);
        }

        protected override string Point64Type(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("point64_t", nullable, option);
        }

        protected override string Size8Type(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("size8_t", nullable, option);
        }

        protected override string Size16Type(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("size16_t", nullable, option);
        }

        protected override string Size32Type(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("size32_t", nullable, option);
        }

        protected override string Size64Type(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("size64_t", nullable, option);
        }

        protected override string Range8Type(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("range8_t", nullable, option);
        }

        protected override string Range16Type(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("range16_t", nullable, option);
        }

        protected override string Range32Type(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("range32_t", nullable, option);
        }

        protected override string Range64Type(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("range64_t", nullable, option);
        }

        public string Build(string type)
        {
            var option = new DataFormatOption();
            return Build(type, null, option);
        }
    }
}
