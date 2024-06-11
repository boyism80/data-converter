using ExcelTableConverter.Model;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Collections;

namespace ExcelTableConverter.Worker.Loader
{
    public class RawDataLoader : ParallelSheetLoader<RawSheetData>
    {
        public RawDataLoader(Context ctx, IReadOnlyList<Sheet> sheets) : base(ctx, sheets)
        {
        }

        private IReadOnlyDictionary<int, ICell> ReadLineUntilValid(IEnumerator enumerator, out int row)
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
                if (line.Count > 0)
                {
                    row = sheetRow.RowNum;
                    return line;
                }
            }
        }

        protected override IEnumerable<RawSheetData> OnWork(Sheet sheet)
        {
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
                Columns = columns.Values.ToList()
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
