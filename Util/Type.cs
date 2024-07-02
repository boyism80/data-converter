using NPOI.SS.Formula.Functions;
using System.Text.RegularExpressions;

namespace ExcelTableConverter.Util
{
    public enum NakeFlag : uint
    { 
        PrimaryKey = 0x00000001,
        GroupKey = 0x00000002,
        Relation = 0x00000004,
        Nullable = 0x00000008,
        Strong = 0x00000010,
        Sequence = 0x00000020,
        Key = PrimaryKey | GroupKey,
        All = 0xFFFFFFFF,
    }

    public static class Type
    {
        private static readonly Regex _pk = new Regex(@"^\*(?<type>.+)$", RegexOptions.Compiled);
        private static readonly Regex _gk = new Regex(@"^\((?<type>.+)\)", RegexOptions.Compiled);
        private static readonly Regex _relation = new Regex(@"^\$(?<type>.+)", RegexOptions.Compiled);
        private static readonly Regex _strong = new Regex(@"\W*!(?<type>.+)$", RegexOptions.Compiled);
        private static readonly Regex _sequence = new Regex(@"\W*~(?<type>.+)$", RegexOptions.Compiled);
        private static readonly Regex _array = new Regex(@"^\[(?<type>.*)\]$", RegexOptions.Compiled);
        private static readonly Regex _map = new Regex(@"^\s*{\s*(?<key>[.\S]+)\s*:\s*(?<value>[.\S]+)\s*}\s*$", RegexOptions.Compiled);
        private static readonly Regex _dsl = new Regex(@"^(?<header>\w+)\((?<parameters>.*)\)$", RegexOptions.Compiled);
        private static readonly Regex _point = new Regex(@"^point(<(?<e>.+)>)?$");
        private static readonly Regex _size = new Regex(@"^size(<(?<e>.+)>)?$");
        private static readonly Regex _range = new Regex(@"^range(<(?<e>.+)>)?$");

        public static string Nake(string type, NakeFlag flag = NakeFlag.All)
        {
            if (flag.HasFlag(NakeFlag.PrimaryKey) && IsPrimaryKey(type, out var pk))
            {
                type = pk;
            }

            if (flag.HasFlag(NakeFlag.GroupKey) && IsGroupKey(type, out var gk))
            {
                type = gk;
            }

            if (flag.HasFlag(NakeFlag.Strong) && IsStrong(type, out var strong))
            {
                type = strong;
            }

            if (flag.HasFlag(NakeFlag.Sequence) && IsSequence(type, out var sequence))
            {
                type = sequence;
            }

            if (flag.HasFlag(NakeFlag.Relation) && IsRelation(type, out var rel))
            {
                type = rel;
            }

            if (flag.HasFlag(NakeFlag.Nullable) && IsNullable(type))
            {
                type = type[..^1];
            }

            return type;
        }

        public static bool IsNullable(string type)
        {
            return type.Trim().EndsWith("?");
        }

        public static string MakeNullable(string type)
        {
            if (IsNullable(type))
                return type;

            return $"{type}?";
        }

        public static bool IsString(string type)
        {
            return Nake(type) == "string";
        }

        public static bool IsPrimaryKey(string value, out string output)
        {
            var match = _pk.Match(value);
            if (match.Success)
            {
                output = match.Groups["type"].Value;
                return true;
            }
            else
            {
                output = string.Empty;
                return false;
            }
        }

        public static bool IsGroupKey(string value, out string output)
        {
            var match = _gk.Match(value);
            if (match.Success)
            {
                output = match.Groups["type"].Value;
                return true;
            }
            else
            {
                output = string.Empty;
                return false;
            }
        }

        public static bool IsStrong(string value, out string output)
        {
            var match = _strong.Match(value);
            if (match.Success)
            {
                output = match.Groups["type"].Value;
                return true;
            }
            else
            {
                output = string.Empty;
                return false;
            }
        }

        public static bool IsSequence(string value, out string output)
        {
            var match = _sequence.Match(value);
            if (match.Success)
            {
                output = match.Groups["type"].Value;
                return true;
            }
            else
            {
                output = string.Empty;
                return false;
            }
        }

        public static bool IsKey(string value, out string output)
        {
            if (IsPrimaryKey(value, out output))
                return true;

            if (IsGroupKey(value, out output))
                return true;

            return false;
        }

        public static bool IsRelation(string value, out string output)
        {
            if (string.IsNullOrEmpty(value))
            {
                output = null;
                return false;
            }

            value = Nake(value, NakeFlag.All & ~(NakeFlag.Relation | NakeFlag.Nullable));

            var match = _relation.Match(value);
            if (match.Success)
            {
                output = match.Groups["type"].Value;
                return true;
            }
            else
            {
                output = string.Empty;
                return false;
            }
        }

        public static bool IsArray(string value, out string output)
        {
            var match = _array.Match(value);
            if (match.Success)
            {
                output = match.Groups["type"].Value;
                return true;
            }
            else
            {
                output = string.Empty;
                return false;
            }
        }

        public static bool IsMap(string value, out KeyValuePair<string, string> output)
        {
            var match = _map.Match(value);
            if (match.Success)
            {
                output = new KeyValuePair<string, string>(match.Groups["key"].Value, match.Groups["value"].Value);
                return true;
            }
            else
            {
                output = new KeyValuePair<string, string>();
                return false;
            }
        }

        public static bool IsDSL(string value, out string header, out List<string> parameters)
        {
            var match = _dsl.Match(value);
            if (match.Success)
            {
                header = match.Groups["header"].Value;
                parameters = match.Groups["parameters"].Value.Split(",").Select(x => x.Trim()).ToList();
                return true;
            }
            else
            {
                header = null;
                parameters = null;
                return false;
            }
        }

        public static bool IsPoint(string value, out string e)
        {
            var match = _point.Match(value);
            if (match.Success)
            {
                if (match.Groups.ContainsKey("e"))
                    e = match.Groups["e"].Value;
                else
                    e = null;
                return true;
            }
            else
            {
                e = null;
                return false;
            }
        }

        public static bool IsSize(string value, out string e)
        {
            var match = _size.Match(value);
            if (match.Success)
            {
                if (match.Groups.ContainsKey("e"))
                    e = match.Groups["e"].Value;
                else
                    e = null;
                return true;
            }
            else
            {
                e = null;
                return false;
            }
        }

        public static bool IsRange(string value, out string e)
        {
            var match = _range.Match(value);
            if (match.Success)
            {
                if (match.Groups.ContainsKey("e"))
                    e = match.Groups["e"].Value;
                else
                    e = null;
                return true;
            }
            else
            {
                e = null;
                return false;
            }
        }
    }
}
