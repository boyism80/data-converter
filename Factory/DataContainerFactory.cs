using ExcelTableConverter.Configuration;
using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory
{
    public class DataContainerFactory
    {
        private readonly uint _scope;

        public DataContainerFactory(uint scope)
        {
            _scope = scope;
        }

        private static DataValue GetKey(Context ctx, string tableName, string name, IReadOnlyDictionary<string, DataValue> row)
        {
            if (row.TryGetValue(name, out var value))
            {
                return value;
            }

            var hasAField = row.Where(x => x.Value is ObjectValue).ToList();
            if (hasAField.Count != 1)
                return null;

            var basedName = hasAField[0].Key;
            if (ctx.Completed.Schema.ContainsKey(basedName) == false)
                return null;

            return GetKey(ctx, basedName, name, ((ObjectValue)hasAField[0].Value).Fields);
        }

        private object InternalBuild(Context ctx, string table, List<Dictionary<string, DataValue>> rows, bool chainParent)
        {
            var schema = ctx.Completed.Schema[table];
            var gk = chainParent ? schema.Values.FirstOrDefault(x => AppConfiguration.ContainsScope(x.Scope, _scope) && x.ColumnType.GroupKey) : null;
            if (gk != null)
            {
                return rows.GroupBy(x => GetKey(ctx, table, gk.Name, x)).ToDictionary(g => g.Key, g => InternalBuild(ctx, table, g.ToList(), false));
            }

            var pk = schema.Values.FirstOrDefault(x => x.ColumnType.PrimaryKey);
            if (pk != null)
            {
                return rows.ToDictionary(x => GetKey(ctx, table, pk.Name, x));
            }

            return rows;
        }

        public object Build(Context ctx, string table, List<Dictionary<string, DataValue>> rows)
        {
            return InternalBuild(ctx, table, rows, true);
        }
    }
}
