using ExcelTableConverter.Model;
using ExcelTableConverter.Util;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Collections;
using System.Text.RegularExpressions;

namespace ExcelTableConverter.Worker.Loader
{
    public class RawDataLoader : ParallelSheetLoader<RawSheetData>
    {
        private static List<string> KEYWORDS = new List<string> { "based", "json" };

        public RawDataLoader(Context ctx, IReadOnlyList<Sheet> sheets) : base(ctx, sheets)
        {
        }

        private IReadOnlyDictionary<int, ICell> ReadLineUntilValid(IEnumerator enumerator, out int row, Func<IReadOnlyDictionary<int, ICell>, bool> predicate = null)
        {
            while (true)
            {
                if (enumerator.MoveNext() == false)
                {
                    row = 0;
                    return null;
                }

                var sheetRow = enumerator.Current as XSSFRow;
                var line = ReadLine(sheetRow);
                if (line.Count == 1)
                {
                    if (GetKeyword(line.Values.First(), out _) != null)
                        continue;
                }

                if (line.Count > 0)
                {
                    if (predicate == null || predicate.Invoke(line))
                    {
                        row = sheetRow.RowNum;
                        return line;
                    }
                }
            }
        }

        private string GetKeyword(ICell cell, out string keyword)
        {
            keyword = null;
            if (cell.CellType != CellType.String)
                return null;

            var value = cell.StringCellValue;
            foreach (var k in KEYWORDS)
            {
                var regex = new Regex($"{k}\\s*:\\s*(?<keyword>[a-zA-Z_][a-zA-Z0-9_]*)");
                var match = regex.Match(value);
                if (match.Success)
                {
                    keyword = k;
                    return match.Groups["keyword"].Value;
                }
            }

            return null;
        }

        private string ReadKeyword(Sheet sheet, string keyword)
        {
            var e = sheet.Raw.GetEnumerator();
            while (e.MoveNext())
            {
                var sheetRow = e.Current as XSSFRow;
                var line = ReadLine(sheetRow);
                if (line.Count != 1)
                    return null;

                if (line.TryGetValue(0, out var cell) == false)
                    return null;

                var result = GetKeyword(cell, out var k);
                if (result != null && keyword == k)
                    return result;
            }

            return null;
        }

        protected override IEnumerable<RawSheetData> OnWork(Sheet sheet)
        {
            var based = ReadKeyword(sheet, "based");
            var json = ReadKeyword(sheet, "json");

            var enumerator = sheet.Raw.GetRowEnumerator();
            var names = ReadLineUntilValid(enumerator, out _);
            var types = ReadLineUntilValid(enumerator, out _);
            var scopes = ReadLineUntilValid(enumerator, out _);
            var columns = new Dictionary<int, RawDataColumns>();

            foreach (var col in names.Keys)
            {
                columns.Add(col, new RawDataColumns
                {
                    Name = names[col].StringCellValue,
                    Type = types[col].StringCellValue,
                    Scope = scopes[col].StringCellValue switch
                    {
                        "server" => Scope.Server,
                        "client" => Scope.Client,
                        "common" => Scope.Common,
                        _ => throw new LogicException("invalid scope type")
                    },
                    Bold = names[col].CellStyle.GetFont(sheet.Parent.Raw).IsBold,
                });
            }

            while (true)
            {
                var line = ReadLineUntilValid(enumerator, out var row);
                if (line == null)
                    break;

                foreach (var col in line.Keys)
                {
                    if (columns.TryGetValue(col, out var column) == false)
                        continue;

                    var value = GetValue(line[col], column.Type);
                    if (value == null)
                        continue;

                    column.RowValuePairs.Add(row, value);
                }
            }

            yield return new RawSheetData
            { 
                Parent = sheet,
                Columns = columns.Values.ToList(),
                Based = based,
                Json = json ?? sheet.GetTableName(),
            };
        }

        protected override void OnWorked(Sheet input, RawSheetData output, int percent)
        {
            if (Context.RawData.TryGetValue(input.Parent.FileName, out var datas) == false)
            {
                datas = new List<RawSheetData>();
                Context.RawData.Add(input.Parent.FileName, datas);
            }
            datas.Add(output);
            Logger.Write($"데이터 테이블을 읽었습니다. - {input.SheetName}", percent: percent);
        }

        protected override IReadOnlyList<RawSheetData> OnFinish(IReadOnlyList<RawSheetData> output)
        {
            Logger.Complete("데이터 테이블을 읽었습니다.");
            return base.OnFinish(output);
        }
    }
}
