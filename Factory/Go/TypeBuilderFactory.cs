using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory.Go
{
    public class TypeBuilderFactory : DataFormatFactory<string>
    {
        public TypeBuilderFactory(Context ctx) : base(ctx)
        {
        }

        private string WithNullable(string root, bool nullable, IExcelFileTrackable tracker)
        {
            root = new TypeFactory(Context).Build(root, tracker);
            root = Util.Type.Nake(root);
            if (nullable)
                root = $"*{root}";

            return root;
        }

        protected override string ArrayType(object value, string root, string e, DataFormatOption option, IExcelFileTrackable tracker)
        {
            var rootType = Context.Completed.Schema.GetRootTableType(e);
            var func = $@"func(rm json.RawMessage) ({new TypeFactory(Context).Build(rootType, tracker)}, error) {{
		return New{Build(e, tracker)}.Build(rm)
    }}";
            return $@"ArrayBuilder({func})";
        }

        protected override string BooleanType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"DefaultBuilder[{WithNullable(root, nullable, tracker)}]()";
        }

        protected override string DateRangeType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return "DateRangeBuilder()";
        }

        protected override string DateTimeType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return "DateTimeBuilder()";
        }

        protected override string DictionaryType(object value, string root, string k, string v, DataFormatOption option, IExcelFileTrackable tracker)
        {
            var rootKeyType = Context.Completed.Schema.GetRootTableType(k);
            var rootValueType = Context.Completed.Schema.GetRootTableType(v);
            var keyFunc = $@"func(rm json.RawMessage) ({new TypeFactory(Context).Build(rootKeyType, tracker)}, error) {{     
		return New{Build(k, tracker)}.Build(rm)
    }}";
            var valueFunc = $@"func(rm json.RawMessage) ({new TypeFactory(Context).Build(rootValueType, tracker)}, error) {{
		return New{Build(v, tracker)}.Build(rm)
    }}";
            return $"DictionaryBuilder({keyFunc}, {valueFunc})";
        }

        protected override string DoubleType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"DefaultBuilder[{WithNullable(root, nullable, tracker)}]()";
        }

        protected override string DslType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"DslBuilder()";
        }

        protected override string EnumType(object value, string root, string e, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"EnumBuilder[{e}]({root.ToLower()}_name_to_value)";
        }

        protected override string FloatType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"DefaultBuilder[{WithNullable("float32", nullable, tracker)}]()";
        }

        protected override string IntType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"DefaultBuilder[{WithNullable("int32", nullable, tracker)}]()";
        }

        protected override string LongType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"DefaultBuilder[{WithNullable("int64", nullable, tracker)}]()";
        }

        protected override string ByteType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"DefaultBuilder[{WithNullable("uint8", nullable, tracker)}]()";
        }

        protected override string SbyteType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"DefaultBuilder[{WithNullable("int8", nullable, tracker)}]()";
        }

        protected override string ShortType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"DefaultBuilder[{WithNullable("int16", nullable, tracker)}]()";
        }

        protected override string UshortType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"DefaultBuilder[{WithNullable("uint16", nullable, tracker)}]()";
        }

        protected override string UintType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"DefaultBuilder[{WithNullable("uint32", nullable, tracker)}]()";
        }

        protected override string UlongType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"DefaultBuilder[{WithNullable("uint64", nullable, tracker)}]()";
        }

        protected override string StringType(object value, string root, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"DefaultBuilder[string]()";
        }

        protected override string TimeSpanType(object value, string root, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"TimeSpanBuilder()";
        }

        protected override string PointType(object value, string root, string e, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"PointBuilder[{WithNullable(e, nullable, tracker)}]()";
        }

        protected override string SizeType(object value, string root, string e, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"SizeBuilder[{WithNullable(e, nullable, tracker)}]()";
        }

        protected override string RangeType(object value, string root, string e, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"RangeBuilder[{WithNullable(e, nullable, tracker)}]()";
        }

        protected override string AreaType(object value, string root, string e, bool nullable, DataFormatOption option, IExcelFileTrackable tracker)
        {
            return $"AreaBuilder[{WithNullable(e, nullable, tracker)}]()";
        }

        public string Build(string type, IExcelFileTrackable tracker = null)
        {
            return base.Build(type, null, null, tracker);
        }
    }
}
