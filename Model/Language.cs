namespace ExcelTableConverter.Model
{
    // How names are spelled in the code of one output language.
    public sealed class Language
    {
        public static readonly Language Cpp = new("C++", "::", x => x, x => x);
        public static readonly Language CSharp = new("C#", ".", UpperCamel, UpperCamel);
        public static readonly Language Go = new("Go", ".", UpperCamel, UpperSnake);
        public static readonly Language Node = new("Node", ".", x => x, x => x);
        public static readonly Language Lua = new("Lua", ".", x => x, UpperSnake);

        private readonly Func<string, string> _identifier;
        private readonly Func<string, string> _constant;

        public string Name { get; }
        public string Separator { get; }

        private Language(string name, string separator, Func<string, string> identifier, Func<string, string> constant)
        {
            Name = name;
            Separator = separator;
            _identifier = identifier;
            _constant = constant;
        }

        public string Identifier(string name) => _identifier(name);
        public string Constant(string name) => _constant(name);
        public string Namespace(Namespace ns) => string.Join(Separator, ns.Segments.Select(_identifier));
        public string LocalNamespace(Namespace ns) => string.Join(Separator, ns.Local.Select(_identifier));
        public string Qualify(Namespace ns, string name) => $"{Namespace(ns)}{Separator}{_identifier(name)}";

        private static string UpperCamel(string value)
        {
            return string.Concat(value.ToLower().Split('_', StringSplitOptions.RemoveEmptyEntries).Select(s => char.ToUpperInvariant(s[0]) + s[1..]));
        }

        private static string UpperSnake(string value) => value.ToUpper().Replace("-", "_");
    }
}
