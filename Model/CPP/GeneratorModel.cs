using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcelTableConverter.Model.CPP
{
    public class ClassCodeGenerationProperty
    {
        public string Name { get; set; }
        public bool Key { get; set; }
        public string Type { get; set; }
        public string Initializer { get; set; }
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
}
