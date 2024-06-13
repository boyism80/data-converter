using ExcelTableConverter.Model;
using Newtonsoft.Json;

namespace ExcelTableConverter.Worker.Generator
{
    // 임시코드로 클라 테스트 끝나면 나중에 합침
    public class JsonSheetFileGenerator : ParallelWorker<(string FileName, object DataSet), bool>
    {
        private string _dir;

        public JsonSheetFileGenerator(Context ctx) : base(ctx)
        {
            _dir = Path.Combine(Context.Output, Context.Config.JsonSheetFilePath);
        }

        protected override IEnumerable<(string FileName, object DataSet)> OnReady()
        {
            if (Directory.Exists(_dir) == false)
                Directory.CreateDirectory(_dir);

            foreach (var file in Directory.GetFiles(_dir))
            {
                File.Delete(file);
            }

            foreach (var dir in Directory.GetDirectories(_dir))
            {
                Directory.Delete(dir, true);
            }

            foreach (var scope in new[] { Scope.Client })
            {
                var dir = Path.Combine(_dir, $"{scope}".ToLower());
                if (Directory.Exists(dir) == false)
                    Directory.CreateDirectory(dir);

                foreach (var file in Directory.GetFiles(dir))
                    File.Delete(file);

                foreach (var (tableName, dataSet) in Context.GetEffectiveSortedDataSet(scope))
                {
                    if (Context.Config.PartitionJsonTables.Contains(tableName))
                        continue;

                    var path = Path.Combine(dir, $"{tableName}.json");
                    yield return (path, dataSet);
                }

                foreach (var (sheetName, dataSet) in Context.GetEffectiveSortedDataSetWithSheetName(scope))
                {
                    foreach (var (tableName, rows) in dataSet)
                    {
                        if (!Context.Config.PartitionJsonTables.Contains(tableName))
                            continue;

                        var fname = string.Empty;
                        var split = sheetName.Split('.');
                        if (split.Length > 1)
                        {
                            fname = $"{tableName}.{string.Join('.', split.Skip(1))}.json";
                        }
                        else
                        {
                            fname = $"{tableName}.json";
                        }
                        var path = Path.Combine(dir, fname);
                        yield return (path, rows);
                    }
                }
            }
        }

        protected override IEnumerable<bool> OnWork((string FileName, object DataSet) value)
        {
            var stringify = JsonConvert.SerializeObject(value.DataSet, Formatting.Indented, new JsonSerializerSettings
            {
                DateFormatString = "yyyy-MM-dd HH:mm:ss"
            });
            File.WriteAllText(value.FileName, stringify);
            yield return true;
        }

        protected override void OnWorked((string FileName, object DataSet) input, bool output, int percent)
        {
            Logger.Write($"json 파일을 저장했습니다. (with sheet) - {input.FileName}", percent: percent);
        }

        protected override IReadOnlyList<bool> OnFinish(IReadOnlyList<bool> output)
        {
            Logger.Complete("json 파일을 저장했습니다. (with sheet)");
            return base.OnFinish(output);
        }
    }
}
