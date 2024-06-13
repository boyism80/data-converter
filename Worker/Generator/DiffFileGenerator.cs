using ExcelTableConverter.Model;
using ExcelTableConverter.Util;
using Newtonsoft.Json;
using System.Text;

namespace ExcelTableConverter.Worker.Generator
{
    public class DiffFileGenerator : ParallelWorker<RawSheetData, bool>
    {
        private readonly string _dir;

        public DiffFileGenerator(Context ctx) : base(ctx)
        {
            _dir = Path.Combine(Context.Output, Context.Config.DiffFilePath);
        }

        protected override IEnumerable<RawSheetData> OnReady()
        {
            if (Directory.Exists(_dir) == false)
                Directory.CreateDirectory(_dir);

            foreach (var file in Directory.GetFiles(_dir))
                File.Delete(file);

            foreach (var rsd in Context.RawData.SelectMany(x => x.Value))
            {
                yield return rsd;
            }
        }

        private void WriteFile(IEnumerable<RawDataColumns> rdcs, string fname)
        {
            var result = rdcs.ToModels();
            var stringify = JsonConvert.SerializeObject(result, Formatting.Indented).Replace("\r\n", "\n");
            var path = Path.Combine(_dir, fname);
            File.WriteAllText(path, stringify, Encoding.UTF8);
        }

        protected override IEnumerable<bool> OnWork(RawSheetData value)
        {
            var (boldColumns, normalColumns) = value.Columns.Split();
            if (boldColumns != null)
            {
                var fname = $"{Path.GetFileNameWithoutExtension(value.FileName)}#{value.SheetName} - Bold.json";
                WriteFile(boldColumns, fname);
            }

            if (normalColumns != null)
            {
                var fname = $"{Path.GetFileNameWithoutExtension(value.FileName)}#{value.SheetName}.json";
                WriteFile(normalColumns, fname);
            }
            yield return true;
        }

        protected override void OnWorked(RawSheetData input, bool output, int percent)
        {
            Logger.Write($"diff 파일을 저장했습니다. - {input.FileName}", percent: percent);
        }

        protected override IReadOnlyList<bool> OnFinish(IReadOnlyList<bool> output)
        {
            Logger.Complete("diff 파일을 저장했습니다.");
            return base.OnFinish(output);
        }
    }
}
