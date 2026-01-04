using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory.CPP
{
    public class InitValueFactory : DataFormatFactory<string>
    {
        public InitValueFactory(Context ctx) : base(ctx)
        {
        }

        private string WithNullable(string root, object value, bool nullable)
        {
            if (nullable)
                root = $"std::optional<{root}>";

            return $"{Util.CPP.Namespace.Access(Context.Configuration.Namespace)}build<{root}>(json[\"{value}\"])";
        }

        protected override string ArrayType(object value, string root, string e, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable($"std::vector<{new TypeFactory(Context).Build(e, tracker)}>", value, false);
        }

        protected override string BooleanType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable("bool", value, nullable);
        }

        protected override string DateRangeType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable($"{Util.CPP.Namespace.Access(Context.Configuration.Namespace)}date_range", value, nullable);
        }

        protected override string DateTimeType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable("datetime", value, nullable);
        }

        protected override string DictionaryType(object value, string root, string k, string v, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable($"std::map<{new TypeFactory(Context).Build(k, tracker)}, {new TypeFactory(Context).Build(v, tracker)}>", value, false);
        }

        protected override string DoubleType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable("double", value, nullable);
        }

        protected override string DslType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable("dsl", value, nullable);
        }

        protected override string EnumType(object value, string root, string e, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable($"{Util.CPP.Namespace.Access(Context.Configuration.Namespace)}{Util.CPP.Namespace.Access(Context.Configuration.EnumNamespace)}{Util.Type.Nake(root)}", value, nullable);
        }

        protected override string FloatType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable("float", value, nullable);
        }

        protected override string IntType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable("int", value, nullable);
        }

        protected override string LongType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable("int64_t", value, nullable);
        }

        protected override string ByteType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable("uint8_t", value, nullable);
        }

        protected override string SbyteType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable("int8_t", value, nullable);
        }

        protected override string ShortType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable("int16_t", value, nullable);
        }

        protected override string UshortType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable("uint16_t", value, nullable);
        }

        protected override string UintType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable("uint32_t", value, nullable);
        }

        protected override string UlongType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable("uint64_t", value, nullable);
        }

        protected override string StringType(object value, string root, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable("std::string", value, false);
        }

        protected override string TimeSpanType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable("timespan", value, nullable);
        }

        protected override string PointType(object value, string root, string e, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable($"point<{new TypeFactory(Context).Build(e, tracker)}>", value, nullable);
        }

        protected override string SizeType(object value, string root, string e, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable($"size<{new TypeFactory(Context).Build(e, tracker)}>", value, nullable);
        }

        protected override string RangeType(object value, string root, string e, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable($"range<{new TypeFactory(Context).Build(e, tracker)}>", value, nullable);
        }

        protected override string AreaType(object value, string root, string e, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return WithNullable($"area<{new TypeFactory(Context).Build(e, tracker)}>", value, nullable);
        }

        public string Build(string type, string name, IExcelFileTrackable tracker = null)
        {
            return base.Build(type, name, null, tracker);
        }
    }
}
