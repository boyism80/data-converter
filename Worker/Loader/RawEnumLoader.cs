using ExcelTableConverter.Model;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using ExcelTableConverter.Util;

namespace ExcelTableConverter.Worker.Loader
{
    public class RawEnumLoader : ParallelSheetLoader<RawEnum>
    {
        public RawEnumLoader(Context ctx, IReadOnlyList<Sheet> sheets) : base(ctx, sheets)
        {
        }

        protected override IEnumerable<RawEnum> OnWork(Sheet sheet)
        {
            var values = new Dictionary<string, List<object>>();
            foreach (XSSFRow row in sheet.Raw)
            {
                var line = ReadLine(row);
                if (line.Count == 0)
                    continue;

                var name = line[0].StringCellValue.Trim();
                var cellType = line[1].CellType;
                if (cellType == CellType.Formula)
                    cellType = line[1].CachedFormulaResultType;

                var value = cellType switch
                {
                    CellType.Numeric => $"{line[1].NumericCellValue}",
                    _ => line[1].StringCellValue.Replace(" ", string.Empty)
                };
                
                var parsed = value.ParseValue();
                if (values.ContainsKey(name))
                    throw new LogicException($"{sheet.FullName}에 {name}이 중복 정의되었습니다.");

                values.Add(name, parsed);
            }

            yield return new RawEnum
            {
                Table = sheet.Name,
                Values = values,
                Parent = sheet
            };
        }

        protected override void OnWorked(Sheet input, RawEnum output, int percent)
        {
            if (Context.RawEnum.TryGetValue(input.Parent.FileName, out var rawEnums) == false)
            {
                rawEnums = new List<RawEnum>();
                Context.RawEnum.Add(input.Parent.FileName, rawEnums);
            }
            rawEnums.Add(output);

            Logger.Write($"열거형 데이터를 읽었습니다. - {input.SheetName}", percent: percent);
        }

        protected override IReadOnlyList<RawEnum> OnFinish(IReadOnlyList<RawEnum> output)
        {
            Logger.Complete("열거형 테이블을 읽었습니다.");
            return base.OnFinish(output);
        }
    }
}
