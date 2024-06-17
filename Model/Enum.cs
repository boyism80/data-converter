using Newtonsoft.Json;

namespace ExcelTableConverter.Model
{
    public class RawEnum : IExcelFileTrackable
    {
        private string _fileName, _sheetName;

        public string Table { get; set; }
        public Dictionary<string, List<object>> Values { get; set; }
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
        [JsonIgnore] public Sheet Parent { get; set; }
    }
}
