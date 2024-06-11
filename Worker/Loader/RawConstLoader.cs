using ExcelTableConverter.Model;
using NPOI.XSSF.UserModel;

namespace ExcelTableConverter.Worker.Loader
{
    public class RawConstLoader : ParallelSheetLoader<RawConst>
    {
        public RawConstLoader(Context ctx, IReadOnlyList<Sheet> sheets) : base(ctx, sheets)
        {
        }

        protected override IEnumerable<RawConst> OnWork(Sheet sheet)
        {
            foreach (XSSFRow row in sheet.Raw)
            {
                var line = ReadLine(row);
                if (line.Count == 0)
                    continue;

                var name = line[0].StringCellValue.Trim();
                var scope = line[1].StringCellValue.Trim() switch
                {
                    "server" => Scope.Server,
                    "client" => Scope.Client,
                    "common" => Scope.Common,
                    _ => throw new LogicException("invalid scope value")
                };
                var type = line[2].StringCellValue;
                var value = GetValue(line[3], type);

                yield return new RawConst
                { 
                    Parent = sheet,
                    Name = name,
                    Scope = scope,
                    Type = type,
                    Value = value
                };
            }
        }

        protected override void OnWorked(Sheet input, RawConst output, int percent)
        {
            if (Context.RawConst.TryGetValue(input.Parent.FileName, out var rawConsts) == false)
            {
                rawConsts = new List<RawConst>();
                Context.RawConst.Add(input.Parent.FileName, rawConsts);
            }
            rawConsts.Add(output);

            Logger.Write($"상수 데이터를 읽었습니다 - {input.SheetName}", percent: percent);
        }

        protected override IReadOnlyList<RawConst> OnFinish(IReadOnlyList<RawConst> output)
        {
            Logger.Complete("상수 테이블을 읽었습니다.");
            return base.OnFinish(output);
        }
    }
}
