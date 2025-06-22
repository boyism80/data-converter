using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory
{
    public class DataContainerFactory
    {
        private readonly Scope _scope;

        public DataContainerFactory(Scope scope)
        {
            _scope = scope;
        }

        private static object GetKey(Context ctx, string tableName, string name, Dictionary<string, object> row)
        {
            if (row.TryGetValue(name, out var value))
            {
                return value;
            }

            var hasAField = row.Where(x => x.Value is Dictionary<string, object>).ToList();
            if (hasAField.Count != 1)
                return null;

            var basedName = hasAField[0].Key;
            if (ctx.Completed.Schema.ContainsKey(basedName) == false)
                return null;

            var basedRow = hasAField[0].Value as Dictionary<string, object>;
            return GetKey(ctx, basedName, name, basedRow);
        }

        private object InternalBuild(Context ctx, string table, List<Dictionary<string, object>> rows, bool chainParent)
        {
            var schema = ctx.Completed.Schema[table];
            var gk = chainParent ? schema.Values.FirstOrDefault(x => x.Scope.HasFlag(_scope) && Util.Type.IsGroupKey(x.Type, out _)) : null;
            if (gk != null)
            {
                return rows.GroupBy(x => GetKey(ctx, table, gk.Name, x)).ToDictionary(g => g.Key, g => InternalBuild(ctx, table, g.ToList(), false));
            }

            var pk = schema.Values.FirstOrDefault(x => Util.Type.IsPrimaryKey(x.Type, out _));
            if (pk != null)
            {
                return rows.ToDictionary(x => GetKey(ctx, table, pk.Name, x));
            }

            return rows;
        }

        public object Build(Context ctx, string table, List<Dictionary<string, object>> rows)
        {
            return InternalBuild(ctx, table, rows, true);
        }
    }
}
