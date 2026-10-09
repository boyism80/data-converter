using ExcelTableConverter.Model;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Text;

namespace ExcelTableConverter.Worker.Cache
{
    public class DataCacheWorker : ParallelWorker<(string FileName, Dictionary<string, List<DataConvertResult>> Data), string>
    {
        public DataCacheWorker(Context ctx) : base(ctx)
        { }

        protected override IEnumerable<(string FileName, Dictionary<string, List<DataConvertResult>> Data)> OnReady()
        {
            foreach (var (fileName, tables) in Context.Completed.Data)
            {
                if (File.Exists(Context.GetCacheFilePath(fileName)) == false)
                    yield return (fileName, tables);
            }
        }

        protected override IEnumerable<string> OnWork((string FileName, Dictionary<string, List<DataConvertResult>> Data) value)
        {
            var path = Context.GetCacheFilePath(value.FileName);
            using (var stream = new GZipStream(File.Create(path), CompressionMode.Compress))
                stream.Write(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value.Data, Context.CACHE_JSON)));
            yield return path;
        }

        protected override void OnWorked((string FileName, Dictionary<string, List<DataConvertResult>> Data) input, string output, int percent)
        {
            Logger.Write($"캐시 파일을 저장했습니다. - {output}");
        }

        protected override IReadOnlyList<string> OnFinish(IReadOnlyList<string> output)
        {
            Logger.Complete($"캐시 파일을 저장했습니다.");
            return base.OnFinish(output);
        }
    }
}
