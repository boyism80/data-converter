using ExcelTableConverter.Util;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace ExcelTableConverter.Model
{
    public class Workbook
    {
        public XSSFWorkbook Source { get; private set; }
        public string Path { get; private set; }
        public string FileName => System.IO.Path.GetFileName(Path);

        public Workbook(XSSFWorkbook source, string path)
        {
            Source = source;
            Path = path;
        }
    }

    public class Sheet : IExcelFileTrackable
    {
        public ISheet Source { get; private set; }
        public Workbook Parent { get; private set; }
        public string Name => this.GetTableName();
        public string FileName => Parent.FileName;
        public string SheetName => Source.SheetName;
        public string FullName => $"{Parent.FileName}:{SheetName}";

        public Sheet(ISheet source, Workbook parent)
        {
            Source = source;
            Parent = parent;
        }
    }
}
