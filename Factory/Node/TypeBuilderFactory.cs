using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory.Node
{
    public class TypeBuilderFactory : DataFormatFactory<string>
    {
        public TypeBuilderFactory(Context ctx) : base(ctx)
        {
        }

        protected override string ArrayType(object value, string root, string e, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"ArrayBuilder({Build(e, tracker)}).build";
        }

        protected override string BooleanType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"DefaultBuilder().build";
        }

        protected override string DateRangeType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return "DateRangeBuilder().build";
        }

        protected override string DateTimeType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return "DateTimeBuilder().build";
        }

        protected override string DictionaryType(object value, string root, string k, string v, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"DictionaryBuilder({Build(k, tracker)}, {Build(v, tracker)}).build";
        }

        protected override string DoubleType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"DefaultBuilder().build";
        }

        protected override string DslType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"DslBuilder().build";
        }

        protected override string EnumType(object value, string root, string e, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"EnumBuilder(\"{e}\").build";
        }

        protected override string FloatType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"DefaultBuilder().build";
        }

        protected override string IntType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"DefaultBuilder().build";
        }

        protected override string LongType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"DefaultBuilder().build";
        }

        protected override string ByteType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"DefaultBuilder().build";
        }

        protected override string SbyteType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"DefaultBuilder().build";
        }

        protected override string ShortType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"DefaultBuilder().build";
        }

        protected override string UshortType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"DefaultBuilder().build";
        }

        protected override string UintType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"DefaultBuilder().build";
        }

        protected override string UlongType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"DefaultBuilder().build";
        }

        protected override string StringType(object value, string root, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"DefaultBuilder().build";
        }

        protected override string TimeSpanType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"TimeSpanBuilder().build";
        }

        protected override string PointType(object value, string root, string e, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"PointBuilder().build";
        }

        protected override string SizeType(object value, string root, string e, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"SizeBuilder().build";
        }

        protected override string RangeType(object value, string root, string e, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"RangeBuilder().build";
        }

        protected override string AreaType(object value, string root, string e, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"AreaBuilder().build";
        }

        public string Build(string type, IExcelFileTrackable tracker = null)
        {
            return base.Build(type, null, null, tracker);
        }
    }
}
