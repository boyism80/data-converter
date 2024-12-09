using ExcelTableConverter.Model;

namespace ExcelTableConverter.Util
{
    public static class Extension
    {
        public static List<Dictionary<string, object>> ToModels(this IEnumerable<RawDataColumns> rdcs)
        {
            var result = new List<Dictionary<string, object>>();
            var rows = rdcs.SelectMany(x => x.RowValuePairs.Keys).Distinct().OrderBy(x => x);
            foreach (var row in rows)
            {
                var data = new Dictionary<string, object>();
                foreach (var rdc in rdcs)
                {
                    var columnName = rdc.Name;
                    var value = rdc.RowValuePairs.GetValueOrDefault(row);
                    data.Add(columnName, value);
                }

                result.Add(data);
            }

            return result;
        }

        public static (IReadOnlyList<RawDataColumns> BoldColumns, IReadOnlyList<RawDataColumns> NormalColumns) Split(this IEnumerable<RawDataColumns> columns)
        {
            var group = columns.GroupBy(x => x.Bold).ToDictionary(x => x.Key);
            var boldColumns = group.GetValueOrDefault(true)?.ToList();
            var normalColumns = group.GetValueOrDefault(false)?.ToList();

            return (boldColumns, normalColumns);
        }
    }
}
