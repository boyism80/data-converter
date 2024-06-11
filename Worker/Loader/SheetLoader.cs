using ExcelTableConverter.Model;

namespace ExcelTableConverter.Worker.Loader
{
    public class SheetLoader : ParallelWorker<Workbook, Sheet>
    {
        private readonly IReadOnlyList<Workbook> _workbooks;
        private readonly bool _quiet;

        public SheetLoader(Context ctx, IReadOnlyList<Workbook> workbooks, bool quiet = false) : base(ctx)
        {
            _quiet = quiet;
            _workbooks = workbooks;
        }

        protected override IEnumerable<Workbook> OnReady()
        {
            foreach (var workbook in _workbooks)
            {
                yield return workbook;
            }
        }

        protected override IEnumerable<Sheet> OnWork(Workbook value)
        {
            for (int i = 0; i < value.Raw.NumberOfSheets; i++)
            {
                if (value.Raw[i].SheetName.StartsWith("#"))
                    yield return null;
                else
                    yield return new Sheet(value.Raw[i], value);
            }
        }

        protected override void OnStart(Workbook input, int percent)
        {
            if (!_quiet)
                Logger.Write($"엑셀 시트를 읽고 있습니다. - {input.FileName}", percent: percent);
        }

        protected override void OnWorked(Workbook input, Sheet output, int percent)
        {
            if (output == null)
                return;

            if (!_quiet)
                Logger.Write($"엑셀 시트를 읽었습니다. - {output.SheetName}", percent: percent);
        }

        protected override int TotalCount(IReadOnlyList<Workbook> inputs)
        {
            return inputs.Select(x => x.Raw.NumberOfSheets).DefaultIfEmpty(0).Sum();
        }

        protected override IReadOnlyList<Sheet> OnFinish(IReadOnlyList<Sheet> output)
        {
            if (!_quiet)
                Logger.Complete("엑셀 시트를 읽었습니다.");
            return output.Where(x => x != null).ToList();
        }
    }
}
