namespace ExcelTableConverter.Model
{
    public class Config
    {
        public List<string> Namespace { get; set; }
        public List<string> EnumNamespace { get; set; }
        public List<string> ConstNamespace { get; set; }
        public string ConstFilePrefix { get; set; }
        public string EnumFilePrefix { get; set; }
        public string JsonFilePath { get; set; }
        public string DiffFilePath { get; set; }
        public string ParentTableFormat { get; set; } = "{0}Attribute";
        public string ParentPropName { get; set; } = "Parent";
        public string DslTypeEnumName { get; set; } = "DslFunctionType";
        public HashSet<string> AdditionalHeaderFiles { get; set; } = new HashSet<string>();
    }
}
