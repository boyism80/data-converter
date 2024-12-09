using ExcelTableConverter.Model;

namespace ExcelTableConverter.Util
{
    public static class Policy
    {
        public static string GetTableName(this IExcelFileTrackable tracker)
        {
            return tracker.SheetName.Split(".").First();
        }
        public static string GetRootName(this IExcelFileTrackable tracker)
        {
            return $"{tracker.FileName}:{tracker.SheetName}";
        }
    }
}
