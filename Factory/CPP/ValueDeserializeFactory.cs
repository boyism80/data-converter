using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory.CPP
{
    public class ValueDeserializeFactory : DataFormatFactory<string>
    {
        public ValueDeserializeFactory(Context ctx) : base(ctx)
        {

        }

        private static string WithOptional(object obj, string type, bool nullable)
        {
            if(nullable && type.EndsWith('&') == false)
                return $"any_cast<{type}&>({obj})";
            else
                return $"any_cast<{type}>({obj})";
        }

        protected override string ArrayType(object obj, string root, string e, DataFormatOption option)
        {
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: true), false);
        }

        protected override string BooleanType(object obj, string root, bool nullable, DataFormatOption option)
        {
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: false), nullable);
        }

        protected override string DateRangeType(object obj, string root, bool nullable, DataFormatOption option)
        {
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: true), nullable);
        }

        protected override string DateTimeType(object obj, string root, bool nullable, DataFormatOption option)
        {
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: true), nullable);
        }

        protected override string DictionaryType(object obj, string root, string k, string v, DataFormatOption option)
        {
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: true), false);
        }

        protected override string DoubleType(object obj, string root, bool nullable, DataFormatOption option)
        {
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: false), nullable);
        }

        protected override string DslType(object obj, string root, bool nullable, DataFormatOption option)
        {
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: true), nullable);
        }

        protected override string EnumType(object obj, string root, string e, bool nullable, DataFormatOption option)
        {
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: false), nullable);
        }

        protected override string FloatType(object obj, string root, bool nullable, DataFormatOption option)
        {
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: false), nullable);
        }

        protected override string IntType(object obj, string root, bool nullable, DataFormatOption option)
        {
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: false), nullable);
        }

        protected override string LongType(object obj, string root, bool nullable, DataFormatOption option)
        {
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: false), nullable);
        }

        protected override string ByteType(object obj, string root, bool nullable, DataFormatOption option)
        {
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: false), nullable);
        }

        protected override string SbyteType(object obj, string root, bool nullable, DataFormatOption option)
        {
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: false), nullable);
        }

        protected override string ShortType(object obj, string root, bool nullable, DataFormatOption option)
        {
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: false), nullable);
        }

        protected override string UshortType(object obj, string root, bool nullable, DataFormatOption option)
        {
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: false), nullable);
        }

        protected override string UintType(object obj, string root, bool nullable, DataFormatOption option)
        {
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: false), nullable);
        }

        protected override string UlongType(object obj, string root, bool nullable, DataFormatOption option)
        {
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: false), nullable);
        }

        protected override string StringType(object obj, string root, DataFormatOption option)
        {
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: true), false);
        }

        protected override string TimeSpanType(object obj, string root, bool nullable, DataFormatOption option)
        {
            return WithOptional(obj, new TypeFactory(Context).Build(root, amp: true), nullable);
        }

        public string Build(string type, string value)
        {
            return base.Build(type, value);
        }
    }
}
