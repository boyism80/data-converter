using ExcelTableConverter.Util;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace ExcelTableConverter.Model
{
    public class Workbook
    { 
        public XSSFWorkbook Raw { get; private set; }
        public string Path { get; private set; }
        public string FileName => System.IO.Path.GetFileName(Path);

        public Workbook(XSSFWorkbook raw, string path)
        {
            Raw = raw;
            Path = path;
        }
    }

    public class Sheet : IExcelFileTrackable
    {
        public ISheet Raw { get; private set; }
        public Workbook Parent { get; private set; }
        public string Name => this.GetTableName();
        public string FileName => Parent.FileName;
        public string SheetName => Raw.SheetName;
        public string FullName => $"{Parent.FileName}:{SheetName}";

        public Sheet(ISheet raw, Workbook parent)
        {
            Raw = raw;
            Parent = parent;
        }
    }
}
