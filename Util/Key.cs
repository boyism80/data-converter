using System.Text.RegularExpressions;

namespace ExcelTableConverter.Util
{
    public enum KeyType
    { 
        None,
        Primary,
        Group
    }

    public static class Key
    {
        private static readonly Regex _regexGK = new Regex(@"^\*(?<type>.+)$", RegexOptions.Compiled);
        private static readonly Regex _regexPK = new Regex(@"^\((?<type>.+)\)$", RegexOptions.Compiled);

        private static string GetGroupKey(string value)
        {
            var match = _regexGK.Match(value);
            if (match.Success)
                return match.Groups["type"].Value;

            return null;
        }

        private static string GetPrimaryKey(string value)
        {
            var match = _regexPK.Match(value);
            if (match.Success)
                return match.Groups["type"].Value;

            return null;
        }

        public static bool Get(this string type, out string key, out KeyType keyType)
        {
            var result = GetGroupKey(type);
            if (string.IsNullOrEmpty(result) == false)
            {
                key = result;
                keyType = KeyType.Group;
                return true;
            }

            result = GetPrimaryKey(type);
            if (string.IsNullOrEmpty(result) == false)
            {
                key = result;
                keyType = KeyType.Primary;
                return true;
            }

            key = string.Empty;
            keyType = KeyType.None;
            return false;
        }
    }
}
