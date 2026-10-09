using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace ExcelTableConverter.Model
{
    // Column type as written in the sheet header: decorations around a value type.
    // *int primary key, (int) group key, !Enum strong, ~int sequence, $table.column relation, int? nullable.
    public sealed record ColumnType
    {
        private static readonly Regex _pk = new Regex(@"^\*(?<type>.+)$", RegexOptions.Compiled);
        private static readonly Regex _gk = new Regex(@"^\((?<type>.+)\)", RegexOptions.Compiled);
        private static readonly Regex _strong = new Regex(@"\W*!(?<type>.+)$", RegexOptions.Compiled);
        private static readonly Regex _sequence = new Regex(@"\W*~(?<type>.+)$", RegexOptions.Compiled);
        private static readonly Regex _relation = new Regex(@"^\$(?<type>.+)", RegexOptions.Compiled);
        private static readonly ConcurrentDictionary<string, ColumnType> _cache = new();

        public string Raw { get; private init; }
        public bool PrimaryKey { get; private init; }
        public bool GroupKey { get; private init; }
        public bool Key => PrimaryKey || GroupKey;
        public bool Sequence { get; private init; }
        public bool Nullable { get; private init; }

        // Raw without the primary/group key decoration.
        public string Unkeyed { get; private init; }

        // Strong link target ("$table" or an enum name), or null.
        public string Strong { get; private init; }

        // Referenced "table" or "table.column", or null.
        public string Relation { get; private init; }

        // Relation split into table and optional column. RelationTable is null when Relation has more than one '.'.
        public string RelationTable { get; private init; }
        public string RelationColumn { get; private init; }

        // Value type with every decoration removed.
        public string Naked { get; private init; }

        // Value type with every decoration removed except nullable.
        public string Value => Nullable ? $"{Naked}?" : Naked;

        public static ColumnType Parse(string raw)
        {
            return _cache.GetOrAdd(raw, _ =>
            {
                var type = raw;
                var pk = _pk.Match(type);
                if (pk.Success)
                    type = pk.Groups["type"].Value;

                var gk = _gk.Match(type);
                if (gk.Success)
                    type = gk.Groups["type"].Value;

                var unkeyed = type;
                var strong = _strong.Match(type);
                if (strong.Success)
                    type = strong.Groups["type"].Value;

                var sequence = _sequence.Match(type);
                if (sequence.Success)
                    type = sequence.Groups["type"].Value;

                var relation = _relation.Match(type);
                if (relation.Success)
                    type = relation.Groups["type"].Value;

                if (type.Trim().EndsWith('?'))
                    type = type[..^1];

                var reference = relation.Success ? TrimNullable(relation.Groups["type"].Value) : null;
                var split = reference?.Split('.');
                var valid = split != null && split.Length <= 2;

                return new ColumnType
                {
                    Raw = raw,
                    PrimaryKey = _pk.IsMatch(raw),
                    GroupKey = _gk.IsMatch(raw),
                    Sequence = _sequence.IsMatch(raw),
                    Nullable = raw.Trim().EndsWith('?'),
                    Unkeyed = unkeyed,
                    Strong = strong.Success ? TrimNullable(strong.Groups["type"].Value) : null,
                    Relation = reference,
                    RelationTable = valid ? split[0] : null,
                    RelationColumn = valid ? split.ElementAtOrDefault(1) : null,
                    Naked = type,
                };
            });
        }

        private static string TrimNullable(string type)
        {
            return type.Trim().EndsWith('?') ? type[..^1] : type;
        }
    }
}
