using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory.Go
{
    public class TypeBuilderFactory : DataFormatFactory<string>
    {
        public TypeBuilderFactory(Context ctx) : base(ctx)
        {
        }

        private string WithNullable(string root, bool nullable)
        {
            root = new TypeFactory(Context).Build(root);
            root = Util.Type.Nake(root);
            if (nullable)
                root = $"*{root}";

            return root;
        }

        protected override string ArrayType(object value, string root, string e, DataFormatOption option)
        {
            var rootType = Context.Completed.Schema.GetRootTableType(e);
            var func = $@"func(rm json.RawMessage) ({new TypeFactory(Context).Build(rootType)}, error) {{
		return New{Build(e)}.Build(rm)
    }}";
            return $@"ArrayBuilder({func})";
        }

        protected override string BooleanType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"DefaultBuilder[{WithNullable(root, nullable)}]()";
        }

        protected override string DateRangeType(object value, string root, bool nullable, DataFormatOption option)
        {
            return "DateRangeBuilder()";
        }

        protected override string DateTimeType(object value, string root, bool nullable, DataFormatOption option)
        {
            return "DateTimeBuilder()";
        }

        protected override string DictionaryType(object value, string root, string k, string v, DataFormatOption option)
        {
            var rootKeyType = Context.Completed.Schema.GetRootTableType(k);
            var rootValueType = Context.Completed.Schema.GetRootTableType(v);
            var keyFunc = $@"func(rm json.RawMessage) ({new TypeFactory(Context).Build(rootKeyType)}, error) {{     
		return New{Build(k)}.Build(rm)
    }}";
            var valueFunc = $@"func(rm json.RawMessage) ({new TypeFactory(Context).Build(rootValueType)}, error) {{
		return New{Build(v)}.Build(rm)
    }}";
            return $"DictionaryBuilder({keyFunc}, {valueFunc})";
        }

        protected override string DoubleType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"DefaultBuilder[{WithNullable(root, nullable)}]()";
        }

        protected override string DslType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"DslBuilder()";
        }

        protected override string EnumType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            return $"EnumBuilder[{e}]({root.ToLower()}_name_to_value)";
        }

        protected override string FloatType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"DefaultBuilder[{WithNullable("float32", nullable)}]()";
        }

        protected override string IntType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"DefaultBuilder[{WithNullable("int32", nullable)}]()";
        }

        protected override string LongType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"DefaultBuilder[{WithNullable("int64", nullable)}]()";
        }

        protected override string ByteType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"DefaultBuilder[{WithNullable("uint8", nullable)}]()";
        }

        protected override string SbyteType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"DefaultBuilder[{WithNullable("int8", nullable)}]()";
        }

        protected override string ShortType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"DefaultBuilder[{WithNullable("int16", nullable)}]()";
        }

        protected override string UshortType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"DefaultBuilder[{WithNullable("uint16", nullable)}]()";
        }

        protected override string UintType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"DefaultBuilder[{WithNullable("uint32", nullable)}]()";
        }

        protected override string UlongType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"DefaultBuilder[{WithNullable("uint64", nullable)}]()";
        }

        protected override string StringType(object value, string root, DataFormatOption option)
        {
            return $"DefaultBuilder[string]()";
        }

        protected override string TimeSpanType(object value, string root, bool nullable, DataFormatOption option)
        {
            return $"TimeSpanBuilder()";
        }

        protected override string PointType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            return $"PointBuilder[{WithNullable(e, nullable)}]()";
        }

        protected override string SizeType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            return $"SizeBuilder[{WithNullable(e, nullable)}]()";
        }

        protected override string RangeType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            return $"RangeBuilder[{WithNullable(e, nullable)}]()";
        }

        protected override string AreaType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            return $"AreaBuilder[{WithNullable(e, nullable)}]()";
        }

        public string Build(string type)
        {
            return base.Build(type, null);
        }
    }
}
