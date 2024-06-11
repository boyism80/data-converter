namespace ExcelTableConverter.Model
{
    public interface IExcelFileTrackable
    {
        string FileName { get; }
        string SheetName { get; }
    }
}
