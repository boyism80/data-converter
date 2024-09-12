using ExcelTableConverter.Model;
using Scriban.Runtime;
using System.Linq;

namespace ExcelTableConverter.Util
{
    public enum LanguageType
    { 
        CPP, CS, NODE
    }

    public class ScribanExtension : ScriptObject
    {
        public static string UpperCamel(string value)
        {
            if (value == null)
                return null;

            return value.ToLower().Split(new[] { "_" }, StringSplitOptions.RemoveEmptyEntries).Select(s => char.ToUpperInvariant(s[0]) + s.Substring(1, s.Length - 1)).Aggregate(string.Empty, (s1, s2) => s1 + s2);
        }

        public static string LowerCamel(string value)
        {
            value = UpperCamel(value);
            if (string.IsNullOrEmpty(value))
                return value;

            return char.ToLowerInvariant(value[0]) + value.Substring(1, value.Length - 1);
        }

        public static string NamespaceAccess(IEnumerable<string> namespaces, LanguageType languageType)
        {
            switch (languageType)
            {
                case LanguageType.CS:
                    return string.Join(".", namespaces);

                case LanguageType.CPP:
                    return string.Join(string.Empty, namespaces.Select(x => $"{x} {{"));

                case LanguageType.NODE:
                    return string.Empty;

                default:
                    return string.Empty;
            }
        }
    }
}
