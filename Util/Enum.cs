using System.Text.RegularExpressions;

namespace ExcelTableConverter.Util
{
    public static class Enum
    {
        public static Match Parse(string value)
        {
            var regex = new Regex(@"^(?<value>[a-zA-Z_]+[a-zA-Z0-9]*|0x[A-F0-9]+|\d+)|(?<op>[&\|])|(?<inv>~)");
            var matched = regex.Match(value);
            return matched;
        }

        public class Comparer : IComparer<KeyValuePair<string, List<object>>>
        {
            public int Compare(KeyValuePair<string, List<object>> val1, KeyValuePair<string, List<object>> val2)
            {
                int num1 = 0, num2 = 0;
                bool isNumeric1 = false, isNumeric2 = false;
                try
                {
                    if (val1.Value.Count == 1)
                    {
                        num1 = Convert.ToInt32($"{val1.Value[0]}", 16);
                        isNumeric1 = true;
                    }
                }
                catch { }
                try
                {
                    if (val2.Value.Count == 1)
                    {
                        num2 = Convert.ToInt32($"{val2.Value[0]}", 16);
                        isNumeric2 = true;
                    }
                }
                catch { }

                if (isNumeric1 && isNumeric2)
                    return num1.CompareTo(num2);
                else if (isNumeric1)
                    return -1;
                else if (isNumeric2)
                    return 1;
                else
                    return string.Join(string.Empty, val1.Value).CompareTo(string.Join(string.Empty, val2.Value));
            }
        }
    }
}
