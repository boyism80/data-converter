namespace ExcelTableConverter.Model
{
    public class DSL
    {
        public string Type { get; set; }
        public List<object> Parameters { get; set; }
    }

    public class DSLParameter
    { 
        public string Type { get; set; }
        public string Name { get; set; }
        public string Desc { get; set; }
    }
}
