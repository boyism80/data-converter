using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory.CPP
{
    public class JsonAppendValueFactory : DataFormatFactory<string>
    {
        public JsonAppendValueFactory(Context ctx) : base(ctx)
        {
        }

        protected override string ArrayType(object value, string root, string e, DataFormatOption option)
        {
            var name = option.Get<string>("name");
            return name;
        }

        protected override string BooleanType(object value, string root, bool nullable, DataFormatOption option)
        {
            var name = option.Get<string>("name");
            if (nullable)
                return $"{name}.has_value() ? {name}.value() : Json::nullValue";
            return name;
        }

        protected override string DateRangeType(object value, string root, bool nullable, DataFormatOption option)
        {
            var name = option.Get<string>("name");
            if (nullable)
                return $"{name}.has_value() ? {name}.value() : Json::nullValue";
            return name;
        }

        protected override string DateTimeType(object value, string root, bool nullable, DataFormatOption option)
        {
            var name = option.Get<string>("name");
            if (nullable)
                return $"{name}.has_value() ? {name}.value() : Json::nullValue";
            return name;
        }

        protected override string DictionaryType(object value, string root, string k, string v, DataFormatOption option)
        {
            var name = option.Get<string>("name");
            return name;
        }

        protected override string DoubleType(object value, string root, bool nullable, DataFormatOption option)
        {
            var name = option.Get<string>("name");
            if (nullable)
                return $"{name}.has_value() ? {name}.value() : Json::nullValue";
            return name;
        }

        protected override string DslType(object value, string root, bool nullable, DataFormatOption option)
        {
            var name = option.Get<string>("name");
            if (nullable)
                return $"{name}.has_value() ? {name}.value().to_json() : Json::nullValue";
            return $"{name}.to_json()";
        }

        protected override string EnumType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            var name = option.Get<string>("name");
            var namespaceAccess = Util.CPP.Namespace.Access(Context.Configuration.Namespace);
            var enumNamespaceAccess = Util.CPP.Namespace.Access(Context.Configuration.EnumNamespace);
            var enumType = $"{namespaceAccess}{enumNamespaceAccess}{e}";

            if (nullable)
            {
                return $"{name}.has_value() ? {namespaceAccess}{enumNamespaceAccess}enum_tostring<{enumType}>({name}.value()) : Json::nullValue";
            }
            else
            {
                return $"{namespaceAccess}{enumNamespaceAccess}enum_tostring<{enumType}>({name})";
            }
        }

        protected override string FloatType(object value, string root, bool nullable, DataFormatOption option)
        {
            var name = option.Get<string>("name");
            if (nullable)
                return $"{name}.has_value() ? {name}.value() : Json::nullValue";
            return name;
        }

        protected override string IntType(object value, string root, bool nullable, DataFormatOption option)
        {
            var name = option.Get<string>("name");
            if (nullable)
                return $"{name}.has_value() ? {name}.value() : Json::nullValue";
            return name;
        }

        protected override string LongType(object value, string root, bool nullable, DataFormatOption option)
        {
            var name = option.Get<string>("name");
            if (nullable)
                return $"{name}.has_value() ? {name}.value() : Json::nullValue";
            return name;
        }

        protected override string ByteType(object value, string root, bool nullable, DataFormatOption option)
        {
            var name = option.Get<string>("name");
            if (nullable)
                return $"{name}.has_value() ? {name}.value() : Json::nullValue";
            return name;
        }

        protected override string SbyteType(object value, string root, bool nullable, DataFormatOption option)
        {
            var name = option.Get<string>("name");
            if (nullable)
                return $"{name}.has_value() ? {name}.value() : Json::nullValue";
            return name;
        }

        protected override string ShortType(object value, string root, bool nullable, DataFormatOption option)
        {
            var name = option.Get<string>("name");
            if (nullable)
                return $"{name}.has_value() ? {name}.value() : Json::nullValue";
            return name;
        }

        protected override string UshortType(object value, string root, bool nullable, DataFormatOption option)
        {
            var name = option.Get<string>("name");
            if (nullable)
                return $"{name}.has_value() ? {name}.value() : Json::nullValue";
            return name;
        }

        protected override string UintType(object value, string root, bool nullable, DataFormatOption option)
        {
            var name = option.Get<string>("name");
            if (nullable)
                return $"{name}.has_value() ? {name}.value() : Json::nullValue";
            return name;
        }

        protected override string UlongType(object value, string root, bool nullable, DataFormatOption option)
        {
            var name = option.Get<string>("name");
            if (nullable)
                return $"{name}.has_value() ? {name}.value() : Json::nullValue";
            return name;
        }

        protected override string StringType(object value, string root, DataFormatOption option)
        {
            var name = option.Get<string>("name");
            return name;
        }

        protected override string TimeSpanType(object value, string root, bool nullable, DataFormatOption option)
        {
            var name = option.Get<string>("name");
            if (nullable)
                return $"{name}.has_value() ? {name}.value() : Json::nullValue";
            return name;
        }

        protected override string PointType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            var name = option.Get<string>("name");
            if (nullable)
                return $"{name}.has_value() ? {name}.value() : Json::nullValue";
            return name;
        }

        protected override string SizeType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            var name = option.Get<string>("name");
            if (nullable)
                return $"{name}.has_value() ? {name}.value() : Json::nullValue";
            return name;
        }

        protected override string RangeType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            var name = option.Get<string>("name");
            if (nullable)
                return $"{name}.has_value() ? {name}.value() : Json::nullValue";
            return name;
        }

        protected override string AreaType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            var name = option.Get<string>("name");
            if (nullable)
                return $"{name}.has_value() ? {name}.value() : Json::nullValue";
            return name;
        }

        public string Build(string type, string name)
        {
            var option = new DataFormatOption();
            option.Add("name", name);
            return Build(type, null, option);
        }
    }
}

