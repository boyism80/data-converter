using ExcelTableConverter.Model;

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
            return root;
        }

        protected override string FloatType(object value, string root, bool nullable, DataFormatOption option)
        {
            return root;
        }

        protected override string IntType(object value, string root, bool nullable, DataFormatOption option)
        {
            return root;
        }

        protected override string LongType(object value, string root, bool nullable, DataFormatOption option)
        {
            return root;
        }

        protected override string ByteType(object value, string root, bool nullable, DataFormatOption option)
        {
            return root;
        }

        protected override string SbyteType(object value, string root, bool nullable, DataFormatOption option)
        {
            return root;
        }

        protected override string ShortType(object value, string root, bool nullable, DataFormatOption option)
        {
            return root;
        }

        protected override string UshortType(object value, string root, bool nullable, DataFormatOption option)
        {
            return root;
        }

        protected override string UintType(object value, string root, bool nullable, DataFormatOption option)
        {
            return root;
        }

        protected override string UlongType(object value, string root, bool nullable, DataFormatOption option)
        {
            return root;
        }

        protected override string StringType(object value, string root, DataFormatOption option)
        {
            return "string";
        }

        protected override string TimeSpanType(object value, string root, bool nullable, DataFormatOption option)
        {
            return root;
        }

        protected override string Point8Type(object value, string root, bool nullable, DataFormatOption option)
        {
            throw new NotImplementedException();
        }

        protected override string Point16Type(object value, string root, bool nullable, DataFormatOption option)
        {
            throw new NotImplementedException();
        }

        protected override string Point32Type(object value, string root, bool nullable, DataFormatOption option)
        {
            throw new NotImplementedException();
        }

        protected override string Point64Type(object value, string root, bool nullable, DataFormatOption option)
        {
            throw new NotImplementedException();
        }

        protected override string Size8Type(object value, string root, bool nullable, DataFormatOption option)
        {
            throw new NotImplementedException();
        }

        protected override string Size16Type(object value, string root, bool nullable, DataFormatOption option)
        {
            throw new NotImplementedException();
        }

        protected override string Size32Type(object value, string root, bool nullable, DataFormatOption option)
        {
            throw new NotImplementedException();
        }

        protected override string Size64Type(object value, string root, bool nullable, DataFormatOption option)
        {
            throw new NotImplementedException();
        }

        protected override string Range8Type(object value, string root, bool nullable, DataFormatOption option)
        {
            throw new NotImplementedException();
        }

        protected override string Range16Type(object value, string root, bool nullable, DataFormatOption option)
        {
            throw new NotImplementedException();
        }

        protected override string Range32Type(object value, string root, bool nullable, DataFormatOption option)
        {
            throw new NotImplementedException();
        }

        protected override string Range64Type(object value, string root, bool nullable, DataFormatOption option)
        {
            throw new NotImplementedException();
        }

        public string Build(string type)
        {
            return Build(type, null);
        }
    }
}
