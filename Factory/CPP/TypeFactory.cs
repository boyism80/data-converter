using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory.CPP
{
    public class TypeFactory : DataFormatFactory<string>
    {
        public TypeFactory(Context ctx) : base(ctx)
        { }

        private string WithNullable(string type, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            var result = nullable ?
                $"std::optional<{Util.Type.Nake(type)}>" : type;

            return result;
        }

        protected override string ArrayType(object value, string root, string e, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"std::vector<{Build(e, tracker)}>";
        }

        protected override string BooleanType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable(root, nullable, option, tracker);
        }

        protected override string DateRangeType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable($"{Util.CPP.Namespace.Access(Context.Configuration.Namespace)}date_range", nullable, option, tracker);
        }

        protected override string DateTimeType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable("datetime", nullable, option, tracker);
        }

        protected override string DictionaryType(object value, string root, string k, string v, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"std::map<{Build(k, tracker)}, {Build(v, tracker)}>";
        }

        protected override string DoubleType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable(root, nullable, option, tracker);
        }

        protected override string DslType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable($"{Util.CPP.Namespace.Access(Context.Configuration.Namespace)}dsl", nullable, option, tracker);
        }

        protected override string EnumType(object value, string root, string e, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable($"{Util.CPP.Namespace.Access(Context.Configuration.Namespace)}{Util.CPP.Namespace.Access(Context.Configuration.EnumNamespace)}{Util.Type.Nake(root)}", nullable, option, tracker);
        }

        protected override string FloatType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable(root, nullable, option, tracker);
        }

        protected override string IntType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable(root, nullable, option, tracker);
        }

        protected override string LongType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable("int64_t", nullable, option, tracker);
        }

        protected override string ByteType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable("uint8_t", nullable, option, tracker);
        }

        protected override string SbyteType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable("int8_t", nullable, option, tracker);
        }

        protected override string ShortType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable("int16_t", nullable, option, tracker);
        }

        protected override string UshortType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable("uint16_t", nullable, option, tracker);
        }

        protected override string UintType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable("uint32_t", nullable, option, tracker);
        }

        protected override string UlongType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable("uint64_t", nullable, option, tracker);
        }

        protected override string StringType(object value, string root, DataFormatOption option, IExcelFileTrackable tracker)
        {
            var nullable = Util.Type.IsNullable(root);
            return WithNullable("std::string", nullable, option, tracker);
        }

        protected override string TimeSpanType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable("timespan", nullable, option, tracker);
        }

        protected override string PointType(object value, string root, string e, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable($"point<{Build(e, tracker)}>", nullable, option, tracker);
        }

        protected override string SizeType(object value, string root, string e, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable($"size<{Build(e, tracker)}>", nullable, option, tracker);
        }

        protected override string RangeType(object value, string root, string e, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable($"range<{Build(e, tracker)}>", nullable, option, tracker);
        }

        protected override string AreaType(object value, string root, string e, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable($"area<{Build(e, tracker)}>", nullable, option, tracker);
        }

        public string Build(string type, IExcelFileTrackable tracker = null)
        {
            var option = new DataFormatOption();
            return Build(type, null, option, tracker);
        }
    }
}
