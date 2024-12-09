namespace ExcelTableConverter.Util.CS
{
    public static class Namespace
    {
        public static string Access(IEnumerable<string> namespaces)
        {
            return string.Join(".", namespaces.Select(x => x));
        }
    }
}
