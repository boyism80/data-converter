using ExcelTableConverter.Model;
using Org.BouncyCastle.Asn1.Cmp;

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

        protected override string ArrayType(object value, string root, string e)
        {
            return $"List<{Build(e)}>";
        }

        protected override string BooleanType(object value, string root, bool nullable)
        {
            return root;
        }

        protected override string DateRangeType(object value, string root, bool nullable)
        {
            return root;
        }

        protected override string DateTimeType(object value, string root, bool nullable)
        {
            return root;
        }

        protected override string DictionaryType(object value, string root, string k, string v)
        {
            return $"Dictionary<{Build(k)}, {Build(v)}>";
        }

        protected override string DoubleType(object value, string root, bool nullable)
        {
            return root;
        }

        protected override string DslType(object value, string root, bool nullable)
        {
            return WithNullable("Dsl", nullable);
        }

        protected override string EnumType(object value, string root, string e, bool nullable)
        {
            return root;
        }

        protected override string FloatType(object value, string root, bool nullable)
        {
            return root;
        }

        protected override string IntType(object value, string root, bool nullable)
        {
            return root;
        }

        protected override string LongType(object value, string root, bool nullable)
        {
            return root;
        }

        protected override string StringType(object value, string root)
        {
            return "string";
        }

        protected override string TimeSpanType(object value, string root, bool nullable)
        {
            return root;
        }

        public string Build(string type)
        {
            return Build(type, null);
        }
    }
}
