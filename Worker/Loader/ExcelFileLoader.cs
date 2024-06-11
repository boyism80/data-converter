using ExcelTableConverter.Model;
using NPOI.XSSF.UserModel;

namespace ExcelTableConverter.Worker.Loader
{
    public class ExcelFileLoader : ParallelWorker<string, Workbook>
    {
        private readonly List<string> _paths = new List<string>();
        private readonly bool _quiet;

        public ExcelFileLoader(Context ctx, List<string> paths, bool quiet = false) : base(ctx)
        {
            _quiet = quiet;
            _paths = paths;
        }

        protected override IEnumerable<string> OnReady()
        {
            foreach (var path in _paths)
            {
                yield return path;
            }
        }

        protected override IEnumerable<Workbook> OnWork(string path)
        {
            Exception error = null;
            Workbook result = null;
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                result = new Workbook(new XSSFWorkbook(fs), path);
            }
            catch (Exception e)
            {
                error = new AggregateException($"{Path.GetFileName(path)} 파일을 여는 과정에서 문제가 발생했습니다.", e);
            }

            if (error != null)
                throw error;
            else
                yield return result;
        }

        protected override void OnWorked(string input, Workbook output, int percent)
        {
            if (!_quiet)
                Logger.Write($"엑셀 파일을 읽었습니다. - {output.FileName}", percent: percent);
        }

        protected override IReadOnlyList<Workbook> OnFinish(IReadOnlyList<Workbook> output)
        {
            if (!_quiet)
                Logger.Complete("엑셀 파일을 읽었습니다.");
            return base.OnFinish(output);
        }
    }
}
