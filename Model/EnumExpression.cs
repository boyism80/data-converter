using Newtonsoft.Json;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ExcelTableConverter.Model
{
    // An enum value as written in the sheets: a token (member name, number or operator) or a parenthesized group.
    [JsonConverter(typeof(Converter))]
    public sealed class EnumExpression : IComparable<EnumExpression>
    {
        private static readonly Regex _hexToken = new Regex(@"^(?<value>[a-zA-Z_]+[a-zA-Z0-9_]*|0x[A-F0-9]+|\d+)|(?<op>[+\-*/&\|])|(?<inv>~)", RegexOptions.Compiled);
        private static readonly Regex _nameToken = new Regex(@"^(?<value>[a-zA-Z_]+[a-zA-Z0-9_]*)|(?<op>[+\-*/&\|])|(?<inv>~)", RegexOptions.Compiled);
        private static readonly string[] _operators = { "+", "-", "*", "/", "&", "|" };

        public string Token { get; }
        public IReadOnlyList<EnumExpression> Items { get; }

        private EnumExpression(string token, IReadOnlyList<EnumExpression> items)
        {
            Token = token;
            Items = items;
        }

        public IEnumerable<string> Names => Items.SelectMany(x => x.Token == null ? x.Names : x.IsOperator ? Enumerable.Empty<string>() : new[] { x.Token });

        private bool IsOperator => _operators.Contains(Token);

        public static bool IsCombined(string text) => _operators.Any(text.Contains);

        // Member definitions may use numbers; data cells may only use member names.
        public static EnumExpression Parse(string text, bool allowNumber = true, IExcelFileTrackable tracker = null)
        {
            text = text.Replace(" ", string.Empty);
            var regex = allowNumber ? _hexToken : _nameToken;
            var stack = new Stack<List<EnumExpression>>();
            stack.Push(new List<EnumExpression>());

            var index = 0;
            while (index < text.Length)
            {
                if (text[index] == '(')
                {
                    stack.Push(new List<EnumExpression>());
                    index++;
                }
                else if (text[index] == ')')
                {
                    var group = stack.Pop();
                    if (stack.Count == 0)
                        throw new LogicException("구문이 잘못됐습니다.", tracker);

                    stack.Peek().Add(new EnumExpression(null, group));
                    index++;
                }
                else
                {
                    var matched = regex.Match(text.Substring(index));
                    if (matched.Success == false)
                        throw new LogicException("구문이 잘못됐습니다.", tracker);

                    stack.Peek().Add(new EnumExpression(matched.Value, []));
                    index += matched.Value.Length;
                }
            }

            if (stack.Count != 1)
                throw new LogicException("구문이 잘못됐습니다.", tracker);

            return new EnumExpression(null, stack.Pop());
        }

        public void Validate(string enumName, IReadOnlyDictionary<string, EnumExpression> members, IExcelFileTrackable tracker)
        {
            var last = Items.LastOrDefault();
            if (last == null || (last.Token != null && _hexToken.Match(last.Token).Groups["value"].Success == false))
                throw new LogicException("구문이 올바르지 않습니다.", tracker);

            for (int i = 0; i < Items.Count; i++)
            {
                var prev = i > 0 ? Items[i - 1] : null;
                var curr = Items[i];
                var prevMatch = prev?.Token == null ? null : _hexToken.Match(prev.Token);

                if (curr.Token == null)
                {
                    if (prev != null && (prev.Token == null || prevMatch.Groups["value"].Success))
                        throw new LogicException("구문이 올바르지 않습니다.", tracker);

                    curr.Validate(enumName, members, tracker);
                    continue;
                }

                var matched = _hexToken.Match(curr.Token);
                if (matched.Groups["value"].Success)
                {
                    var value = matched.Groups["value"].Value;
                    var number = uint.TryParse(value.StartsWith("0x") ? value[2..] : value, NumberStyles.HexNumber, null, out _) || int.TryParse(value, out _);
                    if (number == false && members.ContainsKey(value) == false)
                        throw new LogicException($"{value}는 {enumName}에 존재하지 않는 열거형입니다.", tracker);

                    if (prev != null && (prev.Token == null || prevMatch.Groups["value"].Success))
                        throw new LogicException("구문이 올바르지 않습니다.", tracker);
                }
                else if (matched.Groups["op"].Success)
                {
                    if (prev == null || (prevMatch != null && prevMatch.Groups["op"].Success))
                        throw new LogicException("구문이 올바르지 않습니다.", tracker);
                }
                else if (matched.Groups["inv"].Success)
                {
                    if (prevMatch != null && prevMatch.Groups["inv"].Success)
                        throw new LogicException("구문이 올바르지 않습니다.", tracker);
                }
            }
        }

        public int Evaluate(Func<string, int> number, string enumName, IExcelFileTrackable tracker)
        {
            var stack = new Stack<int>();
            foreach (var token in Postfix())
            {
                if (Priority(token) == 0)
                {
                    stack.Push(number(token));
                    continue;
                }

                var rhs = stack.Pop();
                var lhs = stack.Pop();
                if (token == "/" && rhs == 0)
                    throw new LogicException($"{enumName}에서 0으로 나누기를 시도했습니다.", tracker);

                stack.Push(token switch
                {
                    "+" => lhs + rhs,
                    "-" => lhs - rhs,
                    "*" => lhs * rhs,
                    "/" => lhs / rhs,
                    "&" => lhs & rhs,
                    _ => lhs | rhs,
                });
            }

            return stack.Pop();
        }

        private static int Priority(string op) => op switch
        {
            "*" or "/" => 3,
            "+" or "-" => 2,
            "&" or "|" => 1,
            _ => 0
        };

        private List<string> Postfix()
        {
            var output = new List<string>();
            var ops = new Stack<string>();
            foreach (var item in Items)
            {
                if (item.Token == null)
                {
                    output.AddRange(item.Postfix());
                }
                else if (Priority(item.Token) > 0)
                {
                    while (ops.Count > 0 && Priority(ops.Peek()) >= Priority(item.Token))
                        output.Add(ops.Pop());
                    ops.Push(item.Token);
                }
                else
                {
                    output.Add(item.Token);
                }
            }

            while (ops.Count > 0)
                output.Add(ops.Pop());

            return output;
        }

        // Source code form for the generated enums: every token goes through name, and the outermost parentheses are dropped.
        public string Format(Func<string, string> name)
        {
            string Write(EnumExpression expression)
            {
                if (expression.Token != null)
                    return name(expression.Token);

                var result = new StringBuilder();
                var items = expression.Items;
                if (items.Count > 1)
                    result.Append('(');

                for (var i = 0; i < items.Count; i++)
                {
                    result.Append(Write(items[i]));
                    if (i + 1 < items.Count && items[i].Token != "~")
                        result.Append(' ');
                }

                if (items.Count > 1)
                    result.Append(')');

                return result.ToString();
            }

            var text = Write(this);
            if (text.StartsWith('(') && text.EndsWith(')'))
                text = text[1..^1];

            return text;
        }

        // Single hex-parsable values come first in numeric order; the rest are ordered by their text.
        public int CompareTo(EnumExpression other)
        {
            var isNumber = TryNumber(out var number);
            var otherIsNumber = other.TryNumber(out var otherNumber);
            if (isNumber && otherIsNumber)
                return number.CompareTo(otherNumber);
            else if (isNumber)
                return -1;
            else if (otherIsNumber)
                return 1;
            else
                return Concat().CompareTo(other.Concat());
        }

        private bool TryNumber(out int number)
        {
            number = 0;
            if (Items.Count != 1 || Items[0].Token == null)
                return false;

            var token = Items[0].Token;
            return int.TryParse(token.StartsWith("0x") ? token[2..] : token, NumberStyles.HexNumber, null, out number);
        }

        private string Concat() => string.Concat(Items.Select(x => x.Token ?? x.ToString()));

        // Parse(ToString()) rebuilds the same tree: nested groups keep their parentheses, the root has none.
        public override string ToString()
        {
            return string.Join(" ", Items.Select(x => x.Token ?? $"({x})"));
        }

        private sealed class Converter : JsonConverter<EnumExpression>
        {
            public override void WriteJson(JsonWriter writer, EnumExpression value, JsonSerializer serializer) => writer.WriteValue(value.ToString());

            public override EnumExpression ReadJson(JsonReader reader, Type objectType, EnumExpression existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                return Parse((string)reader.Value);
            }
        }
    }
}
