using System.Text.RegularExpressions;

namespace ExcelTableConverter.Util
{
    public static class Enum
    {
        public static readonly List<string> _operators = new List<string> { "&", "|" };

        public static Match Parse(string value, bool allowHex = true)
        {
            var regex = allowHex ? 
                new Regex(@"^(?<value>[a-zA-Z_]+[a-zA-Z0-9_]*|0x[A-F0-9]+|\d+)|(?<op>[&\|])|(?<inv>~)") :
                new Regex(@"^(?<value>[a-zA-Z_]+[a-zA-Z0-9_]*)|(?<op>[&\|])|(?<inv>~)");
            var matched = regex.Match(value);
            return matched;
        }

        public static List<object> ParseValue(this string value, bool allowHex = true)
        {
            value = value.Replace(" ", string.Empty);

            var index = 0;
            var stack = new Stack<List<object>>();
            stack.Push(new List<object>());

            while (index < value.Length)
            {
                var substr = value.Substring(index);

                if (substr.StartsWith('('))
                {
                    stack.Push(new List<object>());
                    index++;
                }
                else if (substr.StartsWith(')'))
                {
                    var array = stack.Pop();
                    if (stack.Count == 0)
                        throw new LogicException("구문이 잘못됐습니다.");

                    stack.Peek().Add(array);
                    index++;
                }
                else
                {
                    var matched = Util.Enum.Parse(substr, allowHex);
                    if (matched.Success == false)
                        throw new LogicException("구문이 잘못됐습니다.");

                    var current = string.Empty;
                    if (matched.Groups["value"].Success)
                    {
                        current = matched.Groups["value"].Value;
                    }
                    else if (matched.Groups["op"].Success)
                    {
                        current = matched.Groups["op"].Value;
                    }
                    else if (matched.Groups["inv"].Success)
                    {
                        current = matched.Groups["inv"].Value;
                    }
                    else
                    {
                        throw new Exception();
                    }

                    stack.Peek().Add(current);
                    index += current.Length;
                }
            }

            if (stack.TryPop(out var result) == false)
                throw new LogicException("구문이 잘못됐습니다.");

            if (stack.Count > 0)
                throw new LogicException("구문이 잘못됐습니다.");

            return result;
        }

        public static IEnumerable<string> ExtractEnumValues(this List<object> values)
        {
            foreach (var x in values)
            {
                if (x is List<object> arr)
                {
                    foreach (var x2 in ExtractEnumValues(arr))
                        yield return x2;
                }
                else
                {
                    if (_operators.Contains(x as string) == false)
                        yield return x as string;
                }
            }
        }

        public static bool Combined(string value)
        {
            foreach (var op in _operators)
            {
                if (value.Contains(op))
                    return true;
            }

            return false;
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
