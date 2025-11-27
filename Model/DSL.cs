namespace ExcelTableConverter.Model
{
    public class DSL
    {
        public string Header { get; set; }
        public List<object> Params { get; set; }
    }

    public class DSLParameter
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string Desc { get; set; }
    }
}
