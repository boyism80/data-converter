using NPOI.SS.UserModel;
using System.Text.RegularExpressions;

namespace ExcelTableConverter.Util
{
    public static class Value
    {
        private static readonly Regex _const = new Regex(@"^Const:(?<table>[a-zA-Z_]+):(?<name>[a-zA-Z_]+)", RegexOptions.Compiled);
        private static readonly Regex _dsl = new Regex(@"^(?<header>\w+)\((?<parameters>.*)\)$", RegexOptions.Compiled);

        public static bool IsNull(object value)
        {
            if (value == null)
                return true;

            if (value is string s)
            {
                if (string.IsNullOrEmpty(s))
                    return true;

                if (s.Trim() == "null")
                    return true;
            }

            return false;
        }

        public static bool IsConst(object value, out string table, out string name)
        {
            if (value is not string s)
            {
                table = name = null;
                return false;
            }

            var match = _const.Match(s);
            if (match.Success)
            {
                table = match.Groups["table"].Value;
                name = match.Groups["name"].Value;
                return true;
            }
            else
            {
                table = name = null;
                return false;
            }
        }

        public static bool IsDSL(object value, out string header, out List<object> parameters)
        {
            if (value is not string s)
            {
                header = null;
                parameters = null;
                return false;
            }

            var match = _dsl.Match(s);
            if (match.Success)
            {
                header = match.Groups["header"].Value;
                parameters = match.Groups["parameters"].Value.Split(',', StringSplitOptions.TrimEntries).Where(x => string.IsNullOrEmpty(x) == false).Cast<object>().ToList();
                return true;
            }
            else
            {
                header = null;
                parameters = null;
                return false;
            }
        }
    }
}
