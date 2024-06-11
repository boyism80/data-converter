using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory.CPP
{
    public class TypeFactory : DataFormatFactory<string>
    {
        public TypeFactory(Context ctx) : base(ctx)
        {

        }

        private string WithNullable(string type, bool nullable)
        {
            if (nullable)
                return Util.Type.MakeCPPNullable(Util.Type.Nake(type));
            else
                return type;
        }

        protected override string ArrayType(object value, string root, string e)
        {
            return $"std::vector<{Build(e)}>";
        }

        protected override string BooleanType(object value, string root, bool nullable)
        {
            return WithNullable(root, nullable);
        }

        protected override string DateRangeType(object value, string root, bool nullable)
        {
            return WithNullable("fb::model::date_range", nullable);
        }

        protected override string DateTimeType(object value, string root, bool nullable)
        {
            return WithNullable("boost::posix_time::ptime", nullable);
        }

        protected override string DictionaryType(object value, string root, string k, string v)
        {
            return $"std::map<{Build(k)}, {Build(v)}>";
        }

        protected override string DoubleType(object value, string root, bool nullable)
        {
            return WithNullable(root, nullable);
        }

        protected override string DslType(object value, string root, bool nullable)
        {
            return WithNullable("fb::model::dsl", nullable);
        }

        protected override string EnumType(object value, string root, string e, bool nullable)
        {
            if (nullable)
                return Util.Type.MakeCPPNullable($"fb::model::{Util.Type.Nake(root)}");
            else
                return $"fb::model::{root}";
        }

        protected override string FloatType(object value, string root, bool nullable)
        {
            return WithNullable(root, nullable);
        }

        protected override string IntType(object value, string root, bool nullable)
        {
            return WithNullable(root, nullable);
        }

        protected override string LongType(object value, string root, bool nullable)
        {
            return WithNullable(root, nullable);
        }

        protected override string StringType(object value, string root)
        {
            return "std::string";
        }

        protected override string TimeSpanType(object value, string root, bool nullable)
        {
            return WithNullable("std::chrono::duration", nullable);
        }

        public string Build(string type)
        {
            return Build(type, null);
        }
    }
}
