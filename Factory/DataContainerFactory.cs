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

        private object InternalBuild(Context ctx, string table, List<Dictionary<string, object>> data, bool chainParent)
        {
            var schema = ctx.GetScopeSchema(table, _scope, ScopeFilterType.Contains);
            var gk = chainParent ? schema.Values.FirstOrDefault(x => Util.Type.IsGroupKey(x.Type, out _)) : null;
            if (gk != null)
            {
                return data.GroupBy(x => x[gk.Name]).ToDictionary(g => g.Key, g => InternalBuild(ctx, table, g.ToList(), false));
            }

            var pk = schema.Values.FirstOrDefault(x => Util.Type.IsPrimaryKey(x.Type, out _));
            if (pk != null)
            {
                return data.ToDictionary(x => x[pk.Name]);
            }

            return data;
        }

        public object Build(Context ctx, string table, List<Dictionary<string, object>> data)
        {
            var schema = ctx.GetScopeSchema(table, _scope, ScopeFilterType.Contains);
            if (schema == null)
                return null;

            var scopedDatas = data.ConvertAll(d => d.Where(p => schema.ContainsKey(p.Key)).ToDictionary(x => x.Key, x => x.Value));
            return InternalBuild(ctx, table, scopedDatas, true);
        }
    }
}
