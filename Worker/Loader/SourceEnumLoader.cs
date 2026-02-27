using ExcelTableConverter.Model;
using ExcelTableConverter.Util;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace ExcelTableConverter.Worker.Loader
{
    public class SourceEnumLoader : ParallelSheetLoader<SourceEnum>
    {
        public SourceEnumLoader(Context ctx, IReadOnlyList<Sheet> sheets) : base(ctx, sheets)
        {
        }

        protected override IEnumerable<SourceEnum> OnWork(Sheet sheet)
        {
            var values = new Dictionary<string, List<object>>();
            foreach (XSSFRow row in sheet.Source)
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

                var parsed = value.ParseValue(tracker: sheet);
                if (values.ContainsKey(name))
                    throw new LogicException($"{sheet.FullName}에 {name}이 중복 정의되었습니다.");

                values.Add(name, parsed);
            }

            yield return new SourceEnum
            {
                Table = sheet.Name,
                Values = values,
                Parent = sheet
            };
        }

        protected override void OnWorked(Sheet input, SourceEnum output, int percent)
        {
            if (Context.Source.Enum.TryGetValue(input.Parent.FileName, out var sourceEnums) == false)
            {
                sourceEnums = new List<SourceEnum>();
                Context.Source.Enum.Add(input.Parent.FileName, sourceEnums);
            }
            sourceEnums.Add(output);

            Logger.Write($"열거형 데이터를 읽었습니다. - {input.SheetName}");
        }

        protected override IReadOnlyList<SourceEnum> OnFinish(IReadOnlyList<SourceEnum> output)
        {
            Logger.Complete("열거형 테이블을 읽었습니다.");
            return base.OnFinish(output);
        }
    }
}
