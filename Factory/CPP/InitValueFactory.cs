using ExcelTableConverter.Model;
using System;
using System.Data.SqlTypes;

namespace ExcelTableConverter.Factory.CPP
{
    public class InitValueFactory : DataFormatFactory<string>
    {
        public InitValueFactory(Context ctx) : base(ctx)
        {
        }

        private static string WithNullable(string root, object value, bool nullable)
        {
            if (nullable)
                root = $"std::optional<{root}>";

            return $"fb::model::base::build<{root}>(json[\"{value}\"])";
        }

        protected override string ArrayType(object value, string root, string e)
        {
            return WithNullable($"std::vector<{new TypeFactory(Context).Build(e)}>", value, false);
        }

        protected override string BooleanType(object value, string root, bool nullable)
        {
            return WithNullable("bool", value, nullable);
        }

        protected override string DateRangeType(object value, string root, bool nullable)
        {
            return WithNullable("fb::model::date_range", value, nullable);
        }

        protected override string DateTimeType(object value, string root, bool nullable)
        {
            return WithNullable("boost::posix_time::ptime", value, nullable);
        }

        protected override string DictionaryType(object value, string root, string k, string v)
        {
            return WithNullable($"std::map<{new TypeFactory(Context).Build(k)}, {new TypeFactory(Context).Build(v)}>", value, false);
        }

        protected override string DoubleType(object value, string root, bool nullable)
        {
            return WithNullable("double", value, nullable);
        }

        protected override string DslType(object value, string root, bool nullable)
        {
            return WithNullable("dsl", value, nullable);
        }

        protected override string EnumType(object value, string root, string e, bool nullable)
        {
            return WithNullable($"fb::model::{Util.Type.Nake(root)}", value, nullable);
        }

        protected override string FloatType(object value, string root, bool nullable)
        {
            return WithNullable("float", value, nullable);
        }

        protected override string IntType(object value, string root, bool nullable)
        {
            return WithNullable("int", value, nullable);
        }

        protected override string LongType(object value, string root, bool nullable)
        {
            return WithNullable("long", value, nullable);
        }

        protected override string StringType(object value, string root)
        {
            return WithNullable("std::string", value, false);
        }

        protected override string TimeSpanType(object value, string root, bool nullable)
        {
            return WithNullable("std::chrono::milliseconds", value, false);
        }

        public string Build(string type, string name)
        {
            return base.Build(type, name);
        }
    }
}
