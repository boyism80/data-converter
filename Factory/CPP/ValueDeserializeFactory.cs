using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory.CPP
{
    public class ValueDeserializeFactory : DataFormatFactory<string>
    {
        public ValueDeserializeFactory(Context ctx) : base(ctx)
        {

        }

        private static string WithOptional(object value, string type, bool nullable, bool amp)
        {
            if (nullable || amp)
            {
                type = $"const {type}";

                if (type.EndsWith('&') == false)
                    type = $"{type}&";
            }

            return $"any_cast<{type}>({value})";
        }

        protected override string ArrayType(object value, string root, string e, DataFormatOption option)
        {
            var amp = true;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), false, amp);
        }

        protected override string BooleanType(object value, string root, bool nullable, DataFormatOption option)
        {
            var amp = false;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string DateRangeType(object value, string root, bool nullable, DataFormatOption option)
        {
            var amp = true;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string DateTimeType(object value, string root, bool nullable, DataFormatOption option)
        {
            var amp = true;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string DictionaryType(object value, string root, string k, string v, DataFormatOption option)
        {
            var amp = true;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), false, amp);
        }

        protected override string DoubleType(object value, string root, bool nullable, DataFormatOption option)
        {
            var amp = false;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string DslType(object value, string root, bool nullable, DataFormatOption option)
        {
            var amp = true;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string EnumType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            var amp = false;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string FloatType(object value, string root, bool nullable, DataFormatOption option)
        {
            var amp = false;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string IntType(object value, string root, bool nullable, DataFormatOption option)
        {
            var amp = false;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string LongType(object value, string root, bool nullable, DataFormatOption option)
        {
            var amp = false;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string ByteType(object value, string root, bool nullable, DataFormatOption option)
        {
            var amp = false;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string SbyteType(object value, string root, bool nullable, DataFormatOption option)
        {
            var amp = false;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string ShortType(object value, string root, bool nullable, DataFormatOption option)
        {
            var amp = false;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string UshortType(object value, string root, bool nullable, DataFormatOption option)
        {
            var amp = false;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string UintType(object value, string root, bool nullable, DataFormatOption option)
        {
            var amp = false;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string UlongType(object value, string root, bool nullable, DataFormatOption option)
        {
            var amp = false;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string StringType(object value, string root, DataFormatOption option)
        {
            var amp = true;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), false, amp);
        }

        protected override string TimeSpanType(object value, string root, bool nullable, DataFormatOption option)
        {
            var amp = true;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string Point8Type(object value, string root, bool nullable, DataFormatOption option)
        {
            var amp = true;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string Point16Type(object value, string root, bool nullable, DataFormatOption option)
        {
            var amp = true;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string Point32Type(object value, string root, bool nullable, DataFormatOption option)
        {
            var amp = true;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string Point64Type(object value, string root, bool nullable, DataFormatOption option)
        {
            var amp = true;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string Size8Type(object value, string root, bool nullable, DataFormatOption option)
        {
            var amp = true;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string Size16Type(object value, string root, bool nullable, DataFormatOption option)
        {
            var amp = true;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string Size32Type(object value, string root, bool nullable, DataFormatOption option)
        {
            var amp = true;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string Size64Type(object value, string root, bool nullable, DataFormatOption option)
        {
            var amp = true;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string Range8Type(object value, string root, bool nullable, DataFormatOption option)
        {
            var amp = true;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string Range16Type(object value, string root, bool nullable, DataFormatOption option)
        {
            var amp = true;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string Range32Type(object value, string root, bool nullable, DataFormatOption option)
        {
            var amp = true;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string Range64Type(object value, string root, bool nullable, DataFormatOption option)
        {
            var amp = true;
            return WithOptional(value, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        public string Build(string type, string value)
        {
            return base.Build(type, value);
        }
    }
}
