using ExcelTableConverter.Model;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace ExcelTableConverter.Worker.Loader
{
    public class RawEnumLoader : ParallelSheetLoader<RawEnum>
    {
        public RawEnumLoader(Context ctx, IReadOnlyList<Sheet> sheets) : base(ctx, sheets)
        {
        }

        public static List<object> ParseValue(ICell cell)
        {
            var cellType = cell.CellType;
            if (cellType == CellType.Formula)
                cellType = cell.CachedFormulaResultType;

            var value = cellType switch
            {
                CellType.Numeric => $"{cell.NumericCellValue}",
                _ => cell.StringCellValue.Replace(" ", string.Empty)
            };
            var index = 0;
            var stack = new Stack<List<object>>();
            stack.Push(new List<object>());

            while (index < value.Length)
            {
                var substr = value.Substring(index);

                if (substr.StartsWith('('))
                {
                    stack.Push(new List<object>());
                    index++;
                }
                else if (substr.StartsWith(')'))
                {
                    var array = stack.Pop();
                    if (stack.Count == 0)
                        throw new LogicException("구문이 잘못됐습니다.");

                    stack.Peek().Add(array);
                    index++;
                }
                else
                {
                    var matched = Util.Enum.Parse(substr);
                    if (matched.Success == false)
                        throw new LogicException("구문이 잘못됐습니다.");

                    var array = stack.Peek();
                    var current = string.Empty;
                    if (matched.Groups["value"].Success)
                    {
                        current = matched.Groups["value"].Value;
                    }
                    else if (matched.Groups["op"].Success)
                    {
                        current = matched.Groups["op"].Value;
                    }
                    else if (matched.Groups["inv"].Success)
                    {
                        current = matched.Groups["inv"].Value;
                    }
                    else
                    {
                        throw new Exception();
                    }

                    stack.Peek().Add(current);
                    index += current.Length;
                }
            }

            if (stack.TryPop(out var result) == false)
                throw new LogicException("구문이 잘못됐습니다.");

            if (stack.Count > 0)
                throw new LogicException("구문이 잘못됐습니다.");

            return result;
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
                var value = ParseValue(line[1]);

                if (values.ContainsKey(name))
                    throw new LogicException($"{sheet.FullName}에 {name}이 중복 정의되었습니다.");

                values.Add(name, value);
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
