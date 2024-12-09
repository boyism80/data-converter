namespace ExcelTableConverter.Model
{
    public enum Scope
    { 
        Server = 0x00000001, 
        Client = 0x00000002,
        Common = Server | Client
    }

    public enum ScopeFilterType
    { 
        Contains,
        Match
    }

    public class SchemaData
    { 
        public string Name { get; set; }
        public string Type { get; set; }
        public Scope Scope { get; set; }
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
                var gk = Values.FirstOrDefault(x => Util.Type.IsGroupKey(x.Type, out _));
                if (gk != null)
                    return gk.Name;

                var pk = Values.FirstOrDefault(x => Util.Type.IsPrimaryKey(x.Type, out _));
                if (pk != null)
                    return pk.Name;

                return null;
            }
        }
    }
}
