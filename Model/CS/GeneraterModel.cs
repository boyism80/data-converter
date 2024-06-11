namespace ExcelTableConverter.Model.CS
{
    public class ClassCodeGenerationProperty
    {
        public string Name { get; set; }
        public bool Key { get; set; }
        public int Index { get; set; }
        public string Type { get; set; }
    }

    public class BindingCodeGeneratorProperty
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Generic { get; set; }
    }

    public class ConstCodeGeneratorProperty
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class CMPResolverCodeGeneratorProperty
    {
        public int Index { get; set; }
        public string Type { get; set; }
        public string Generic { get; set; }
        public string Name { get; set; }
    }

    public class DslCodeGeneratorProperty
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string Serialize { get; set; }
        public string Deserialize { get; set; }
    }
}
