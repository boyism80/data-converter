using ExcelTableConverter.Model;
using Newtonsoft.Json;

namespace ExcelTableConverter.Worker.Generator
{
    public class JsonFileGenerator : ParallelWorker<(string FileName, object DataSet), bool>
    {
        public JsonFileGenerator(Context ctx) : base(ctx)
        {
            
        }

        protected override IEnumerable<(string FileName, object DataSet)> OnReady()
        {
            foreach (var scope in new[] { Scope.Server, Scope.Client })
            {
                var dir = Path.Combine(Context.Output, Context.Config.JsonFilePath, $"{scope}".ToLower());
                if (Directory.Exists(dir) == false)
                    Directory.CreateDirectory(dir);

                foreach (var file in Directory.GetFiles(dir))
                    File.Delete(file);

                foreach (var (tableName, dataSet) in Context.GetEffectiveSortedDataSet(scope))
                {
                    var path = Path.Combine(dir, $"{tableName}.json");
                    yield return (path, dataSet);
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
            Logger.Write($"json 파일을 저장했습니다. - {input.FileName}", percent: percent);
        }

        protected override IReadOnlyList<bool> OnFinish(IReadOnlyList<bool> output)
        {
            Logger.Complete("json 파일을 저장했습니다.");
            return base.OnFinish(output);
        }
    }
}
