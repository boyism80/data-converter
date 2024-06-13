namespace ExcelTableConverter.Model
{
    public class Config
    {
        public List<string> Namespace { get; set; }
        public string ConstFilePrefix { get; set; }
        public string EnumFilePrefix { get; set; }
        public string JsonFilePath { get; set; }
        public string JsonSheetFilePath { get; set; }
        public string DiffFilePath { get; set; }
        public string ClassCodeFilePath { get; set; }
        public string BindingCodeFilePath { get; set; }
        public string ConstCodeFilePath { get; set; }
        public string EnumCodeFilePath { get; set; }
        public string CMPResolverCodeFilePath { get; set; }
        public string DslCodeFilePath { get; set; }
        public List<string> SharedJsonFiles { get; set; }
        public HashSet<string> PartitionJsonTables { get; set; }
    }
}
