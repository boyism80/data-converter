using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory.CPP
{
    public class ValueDeserializeFactory : DataFormatFactory<string>
    {
        public ValueDeserializeFactory(Context ctx) : base(ctx)
        {

        }

        private static string WithOptional(object obj, string type, bool nullable, bool amp)
        {
            if (nullable || amp)
            {
                type = $"const {type}";

                if (type.EndsWith('&') == false)
                    type = $"{type}&";
            }

            return $"any_cast<{type}>({obj})";
        }

        protected override string ArrayType(object obj, string root, string e, DataFormatOption option)
        {
            var amp = true;
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: amp), false, amp);
        }

        protected override string BooleanType(object obj, string root, bool nullable, DataFormatOption option)
        {
            var amp = false;
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string DateRangeType(object obj, string root, bool nullable, DataFormatOption option)
        {
            var amp = true;
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string DateTimeType(object obj, string root, bool nullable, DataFormatOption option)
        {
            var amp = true;
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string DictionaryType(object obj, string root, string k, string v, DataFormatOption option)
        {
            var amp = true;
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: amp), false, amp);
        }

        protected override string DoubleType(object obj, string root, bool nullable, DataFormatOption option)
        {
            var amp = false;
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string DslType(object obj, string root, bool nullable, DataFormatOption option)
        {
            var amp = true;
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string EnumType(object obj, string root, string e, bool nullable, DataFormatOption option)
        {
            var amp = false;
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string FloatType(object obj, string root, bool nullable, DataFormatOption option)
        {
            var amp = false;
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string IntType(object obj, string root, bool nullable, DataFormatOption option)
        {
            var amp = false;
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string LongType(object obj, string root, bool nullable, DataFormatOption option)
        {
            var amp = false;
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string ByteType(object obj, string root, bool nullable, DataFormatOption option)
        {
            var amp = false;
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string SbyteType(object obj, string root, bool nullable, DataFormatOption option)
        {
            var amp = false;
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string ShortType(object obj, string root, bool nullable, DataFormatOption option)
        {
            var amp = false;
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string UshortType(object obj, string root, bool nullable, DataFormatOption option)
        {
            var amp = false;
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string UintType(object obj, string root, bool nullable, DataFormatOption option)
        {
            var amp = false;
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string UlongType(object obj, string root, bool nullable, DataFormatOption option)
        {
            var amp = false;
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        protected override string StringType(object obj, string root, DataFormatOption option)
        {
            var amp = true;
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: amp), false, amp);
        }

        protected override string TimeSpanType(object obj, string root, bool nullable, DataFormatOption option)
        {
            var amp = true;
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: amp), nullable, amp);
        }

        public string Build(string type, string value)
        {
            return base.Build(type, value);
        }
    }
}
