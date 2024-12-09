using ExcelTableConverter.Model;
using ExcelTableConverter.Util;

namespace ExcelTableConverter.Factory.CS
{
    public class TypeFactory : DataFormatFactory<string>
    {
        public TypeFactory(Context ctx) : base(ctx)
        {

        }

        private string WithNullable(string type, bool nullable)
        {
            if (nullable)
                return Util.Type.MakeNullable(type);
            else
                return type;
        }

        protected override string ArrayType(object value, string root, string e, DataFormatOption option)
        {
            return $"List<{Build(e)}>";
        }

        protected override string BooleanType(object value, string root, bool nullable, DataFormatOption option)
        {
            return root;
        }

        protected override string DateRangeType(object value, string root, bool nullable, DataFormatOption option)
        {
            return root;
        }

        protected override string DateTimeType(object value, string root, bool nullable, DataFormatOption option)
        {
            return root;
        }

        protected override string DictionaryType(object value, string root, string k, string v, DataFormatOption option)
        {
            return $"Dictionary<{Build(k)}, {Build(v)}>";
        }

        protected override string DoubleType(object value, string root, bool nullable, DataFormatOption option)
        {
            return root;
        }

        protected override string DslType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("Dsl", nullable);
        }

        protected override string EnumType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            var namespaces = Context.Config.Namespace.Concat(Context.Config.EnumNamespace).Select(x => ScribanEx.UpperCamel(x));
            var prefix = ScribanEx.NamespaceAccess(namespaces, LanguageType.CS);

            return $"{prefix}.{ScribanEx.UpperCamel(Util.Type.Nake(root))}";
        }

        protected override string FloatType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("float", nullable);
        }

        protected override string IntType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("int", nullable);
        }

        protected override string LongType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("long", nullable);
        }

        protected override string ByteType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("byte", nullable);
        }

        protected override string SbyteType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("sbyte", nullable);
        }

        protected override string ShortType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("short", nullable);
        }

        protected override string UshortType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("ushort", nullable);
        }

        protected override string UintType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("uint", nullable);
        }

        protected override string UlongType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("ulong", nullable);
        }

        protected override string StringType(object value, string root, DataFormatOption option)
        {
            return "string";
        }

        protected override string TimeSpanType(object value, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable("TimeSpan", nullable);
        }

        protected override string PointType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            return WithNullable($"Point<{Build(e)}>", nullable);
        }

        protected override string SizeType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            return WithNullable($"Size<{Build(e)}>", nullable);
        }

        protected override string RangeType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            return WithNullable($"Range<{Build(e)}>", nullable);
        }

        public string Build(string type)
        {
            return Build(type, null);
        }
    }
}
