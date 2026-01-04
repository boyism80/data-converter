using ExcelTableConverter.Model;
using NPOI.XSSF.UserModel;

namespace ExcelTableConverter.Worker.Loader
{
    public class SourceConstLoader : ParallelSheetLoader<SourceConst>
    {
        public SourceConstLoader(Context ctx, IReadOnlyList<Sheet> sheets) : base(ctx, sheets)
        {
        }

        protected override IEnumerable<SourceConst> OnWork(Sheet sheet)
        {
            foreach (XSSFRow row in sheet.Source)
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

                yield return new SourceConst
                {
                    Parent = sheet,
                    Name = name,
                    Scope = scope,
                    Type = type,
                    Value = value
                };
            }
        }

        protected override void OnWorked(Sheet input, SourceConst output, int percent)
        {
            if (Context.Source.Const.TryGetValue(input.Parent.FileName, out var sourceConsts) == false)
            {
                sourceConsts = new List<SourceConst>();
                Context.Source.Const.Add(input.Parent.FileName, sourceConsts);
            }
            sourceConsts.Add(output);

            Logger.Write($"상수 데이터를 읽었습니다 - {input.SheetName}");
        }

        protected override IReadOnlyList<SourceConst> OnFinish(IReadOnlyList<SourceConst> output)
        {
            Logger.Complete("상수 테이블을 읽었습니다.");
            return base.OnFinish(output);
        }
    }
}
