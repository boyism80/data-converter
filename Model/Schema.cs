using Newtonsoft.Json;

namespace ExcelTableConverter.Model
{
    public enum ScopeFilterType
    {
        Contains,
        Match
    }

    public class SchemaData
    {
        public string Name { get; set; }
        public string Type { get; set; }
        [JsonIgnore] public ColumnType ColumnType => ColumnType.Parse(Type);
        public uint Scope { get; set; }
        public bool Inherited { get; set; }
    }

    public class SchemaSet : Dictionary<string, SchemaData>
    {
        public string Based { get; private set; }
        public string Json { get; private set; }

        public SchemaSet(string based, string json)
        {
            Based = based;
            Json = json;
        }

        public string Key
        {
            get
            {
                var gk = Values.FirstOrDefault(x => x.ColumnType.GroupKey);
                if (gk != null)
                    return gk.Name;

                var pk = Values.FirstOrDefault(x => x.ColumnType.PrimaryKey);
                if (pk != null)
                    return pk.Name;

                return null;
            }
        }
    }
}
