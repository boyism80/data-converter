namespace ExcelTableConverter.Util.CPP
{
    public static class Namespace
    {
        public static string Begin(List<string> namespaces)
        {
            return string.Join(' ', namespaces.Select(n => $"namespace {n} {{"));
        }

        public static string End(List<string> namespaces)
        {
            return string.Join(' ', namespaces.Select(n => $"}}"));
        }

        public static string Access(List<string> namespaces)
        {
            return string.Join("", namespaces.Select(n => $"{n}::"));
        }
    }
}
