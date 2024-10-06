using ExcelTableConverter.Model;
using ExcelTableConverter.Util;

namespace ExcelTableConverter.Factory.CS
{
    public class ValueDeserializeFactory : DataFormatFactory<string>
    {
        public ValueDeserializeFactory(Context ctx) : base(ctx)
        {

        }

        private static string WithNullable(string obj, string result, bool nullable)
        {
            var prefix = nullable ? $"{obj} == null ? null : " : string.Empty;
            return $"{prefix}{result}";
        }

        protected override string ArrayType(object obj, string root, string e, DataFormatOption option)
        {
            return $"({obj} as object[]).Select(x => {Build(e, "x")}).ToList()";
        }

        protected override string BooleanType(object obj, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable(obj as string, $"({new TypeFactory(Context).Build(root)})System.Convert.ChangeType({obj}, typeof({root}))", nullable);
        }

        protected override string DateRangeType(object obj, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable(obj as string, $"DateRange.Parse({obj})", nullable);
        }

        protected override string DateTimeType(object obj, string root, bool nullable, DataFormatOption option)
        {
            return WithNullable(obj as string, $"DateTime.Parse({obj}.ToString())", nullable);
        }

        protected override string DictionaryType(object obj, string root, string k, string v, DataFormatOption option)
        {
            // TODO: 디버깅 후 다시 작성
            return $"({obj} as object[]).Select(x => {Build(k, "x")}).ToList()";
        }

        protected override string DoubleType(object obj, string root, bool nullable, DataFormatOption option)
        {
            var result = $"(double){obj}";
            if (nullable)
                result = $"(double?){result}";

            return WithNullable(obj as string, result, nullable);
        }

        protected override string DslType(object obj, string root, bool nullable, DataFormatOption option)
        {
            var result = $"Newtonsoft.Json.JsonConvert.DeserializeObject<Dsl>(Newtonsoft.Json.JsonConvert.SerializeObject({obj}))";
            if (nullable)
                result = $"(Dsl?){result}";
            return WithNullable(obj as string, result, nullable);
        }

        protected override string EnumType(object obj, string root, string e, bool nullable, DataFormatOption option)
        {
            var namespaces = Context.Config.Namespace.Concat(Context.Config.EnumNamespace).Select(x => ScribanEx.UpperCamel(x));
            var prefix = ScribanEx.NamespaceAccess(namespaces, LanguageType.CS);
            return WithNullable(obj as string, $"({prefix}.{ScribanEx.UpperCamel(root)})Enum.Parse(typeof({prefix}.{ScribanEx.UpperCamel(root)}), {obj}.ToString())", nullable);
        }

        protected override string FloatType(object obj, string root, bool nullable, DataFormatOption option)
        {
            var result = $"(float)(double){obj}";
            if (nullable)
                result = $"(float?){result}";

            return WithNullable(obj as string, result, nullable);
        }

        protected override string IntType(object obj, string root, bool nullable, DataFormatOption option)
        {
            var result = $"(int)(long){obj}";
            if (nullable)
                result = $"(int?){result}";

            return WithNullable(obj as string, result, nullable);
        }

        protected override string LongType(object obj, string root, bool nullable, DataFormatOption option)
        {
            var result = $"(long){obj}";
            if (nullable)
                result = $"(long?){result}";

            return WithNullable(obj as string, result, nullable);
        }

        protected override string ByteType(object obj, string root, bool nullable, DataFormatOption option)
        {
            var result = $"(byte){obj}";
            if (nullable)
                result = $"(byte?){result}";

            return WithNullable(obj as string, result, nullable);
        }

        protected override string SbyteType(object obj, string root, bool nullable, DataFormatOption option)
        {
            var result = $"(ubyte){obj}";
            if (nullable)
                result = $"(ubyte?){result}";

            return WithNullable(obj as string, result, nullable);
        }

        protected override string ShortType(object obj, string root, bool nullable, DataFormatOption option)
        {
            var result = $"(short){obj}";
            if (nullable)
                result = $"(short?){result}";

            return WithNullable(obj as string, result, nullable);
        }

        protected override string UshortType(object obj, string root, bool nullable, DataFormatOption option)
        {
            var result = $"(ushort){obj}";
            if (nullable)
                result = $"(ushort?){result}";

            return WithNullable(obj as string, result, nullable);
        }

        protected override string UintType(object obj, string root, bool nullable, DataFormatOption option)
        {
            var result = $"(uint){obj}";
            if (nullable)
                result = $"(uint?){result}";

            return WithNullable(obj as string, result, nullable);
        }

        protected override string UlongType(object obj, string root, bool nullable, DataFormatOption option)
        {
            var result = $"(ulong){obj}";
            if (nullable)
                result = $"(ulong?){result}";

            return WithNullable(obj as string, result, nullable);
        }

        protected override string StringType(object obj, string root, DataFormatOption option)
        {
            return $"{obj}?.ToString()";
        }

        protected override string TimeSpanType(object obj, string root, bool nullable, DataFormatOption option)
        {
            var result = $"TimeSpan.Parse({obj}.ToString())";
            if (nullable)
                result = $"(TimeSpan?){result}";

            return WithNullable(obj as string, result, nullable);
        }

        public string Build(string type, string value)
        {
            return base.Build(type, value);
        }

        protected override string PointType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            throw new NotImplementedException();
        }

        protected override string SizeType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            throw new NotImplementedException();
        }

        protected override string RangeType(object value, string root, string e, bool nullable, DataFormatOption option)
        {
            throw new NotImplementedException();
        }
    }
}
