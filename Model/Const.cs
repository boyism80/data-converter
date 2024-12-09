using Newtonsoft.Json;

namespace ExcelTableConverter.Model
{
    public class RawConst : IExcelFileTrackable
    {
        private string _fileName, _sheetName, _tableName;

        public string Name { get; set; }
        public string Type { get; set; }
        public object Value { get; set; }
        public Scope Scope { get; set; }

        public string FileName
        {
            get => Parent?.FileName ?? _fileName;
            set => _fileName = value;
        }
        public string SheetName
        {
            get => Parent?.SheetName ?? _sheetName;
            set => _sheetName = value;
        }

        public string TableName
        {
            get => Parent?.Name ?? _tableName;
            set => _tableName = value;
        }

        [JsonIgnore] public Sheet Parent { get; set; }
    }

    public class ConstData
    { 
        public string Name { get; set; }
        public object Value { get; set; }
        public string Type { get; set; }
        public Scope Scope { get; set; }
    }
}
