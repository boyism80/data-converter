using ExcelTableConverter.Model;
using System.Text.RegularExpressions;

namespace ExcelTableConverter.Worker.Validator
{
    public class NameValidateItem : IExcelFileTrackable
    {
        public string FileName { get; set; }
        public string SheetName { get; set; }
        public string Name { get; set; }
    }

    public class NameValidator : ParallelWorker<(IExcelFileTrackable Tracker, string Name), bool>
    {
        private HashSet<string> _files;
        private readonly Regex _regex = new Regex(@"^[a-zA-Z_$][a-zA-Z_$0-9]*$", RegexOptions.Compiled);

        public NameValidator(Context ctx, List<string> files) : base(ctx)
        {
            _files = files.ToHashSet();
        }

        protected override IEnumerable<(IExcelFileTrackable, string)> OnReady()
        {
            foreach (var rawEnums in Context.RawEnum.Values)
            {
                foreach (var rawEnum in rawEnums)
                {
                    if (_files.Contains(rawEnum.FileName) == false)
                        continue;

                    foreach (var name in rawEnum.Values.Keys)
                        yield return (rawEnum, name);
                }
            }

            foreach (var rawConsts in Context.RawConst.Values)
            {
                foreach (var rawConst in rawConsts)
                {
                    if (_files.Contains(rawConst.FileName) == false)
                        continue;

                    yield return (rawConst, rawConst.Name);
                }
            }

            foreach (var rawSheetData in Context.RawData.SelectMany(x => x.Value))
            {
                if (_files.Contains(rawSheetData.FileName) == false)
                    continue;

                foreach (var column in rawSheetData.Columns)
                {
                    yield return (rawSheetData, column.Name);
                }
            }
        }

        protected override IEnumerable<bool> OnWork((IExcelFileTrackable Tracker, string Name) value)
        {
            if (_regex.IsMatch(value.Name) == false)
                throw new LogicException($"{value.Name}은 사용할 수 없는 이름입니다.", value.Tracker);
            
            yield return true;
        }

        protected override void OnWorked((IExcelFileTrackable Tracker, string Name) input, bool output, int percent)
        {
            Logger.Write($"데이터 이름 규칙을 검사중입니다. - {input.Name}", percent: percent);
        }

        protected override IReadOnlyList<bool> OnFinish(IReadOnlyList<bool> output)
        {
            Logger.Complete($"데이터 이름 규칙 검사를 완료했습니다.");
            return base.OnFinish(output);
        }

        protected override void OnError((IExcelFileTrackable Tracker, string Name) input, Exception e, IExcelFileTrackable tracker = null)
        {
            base.OnError(input, e, tracker);
        }
    }
}
