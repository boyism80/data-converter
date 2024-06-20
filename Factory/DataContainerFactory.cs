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

        private object InternalBuild(Context ctx, string table, List<Dictionary<string, object>> rows, bool chainParent)
        {
            var schema = ctx.Result.Schema[table];
            var gk = chainParent ? schema.Values.FirstOrDefault(x => x.Scope.HasFlag(_scope) && Util.Type.IsGroupKey(x.Type, out _)) : null;
            if (gk != null)
            {
                return rows.GroupBy(x => x[gk.Name]).ToDictionary(g => g.Key, g => InternalBuild(ctx, table, g.ToList(), false));
            }

            var pk = schema.Values.FirstOrDefault(x => Util.Type.IsPrimaryKey(x.Type, out _));
            if (pk != null)
            {
                return rows.ToDictionary(x => x[pk.Name]);
            }

            return rows;
        }

        public object Build(Context ctx, string table, List<Dictionary<string, object>> rows)
        {
            return InternalBuild(ctx, table, rows, true);
        }
    }
}
