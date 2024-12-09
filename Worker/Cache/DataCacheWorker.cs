using ExcelTableConverter.Model;
using ExcelTableConverter.Util;

namespace ExcelTableConverter.Worker.Cache
{
    using CacheDataType = Dictionary<string, List<DataConvertResult>>; // TableName : Rows

    public class DataCacheWorker : ParallelWorker<(string FileName, CacheDataType Data), (string FileName, byte[] Bytes)>
    {
        private readonly HashSet<string> _updatedFiles = new HashSet<string>();

        public DataCacheWorker(Context ctx, List<string> updatedFiles) : base(ctx)
        {
            _updatedFiles = updatedFiles.ToHashSet();
        }

        protected override IEnumerable<(string FileName, CacheDataType Data)> OnReady()
        {
            foreach (var (fileName, pairs) in Context.Result.Data)
            {
                if (File.Exists(Context.GetCacheFilePath(fileName)) && _updatedFiles.Contains(fileName) == false)
                    continue;

                yield return (fileName, pairs);
            }
        }

        protected override IEnumerable<(string FileName, byte[] Bytes)> OnWork((string FileName, CacheDataType Data) value)
        {
            var bytes = ZipUtil.Zip(value.Data);
            var cacheFileName = Context.GetCacheFilePath(value.FileName);
            File.WriteAllBytes(cacheFileName, bytes);
            yield return (cacheFileName, bytes);
        }

        protected override void OnWorked((string FileName, CacheDataType Data) input, (string FileName, byte[] Bytes) output, int percent)
        {
            Logger.Write($"캐시 파일을 저장했습니다. - {output.FileName}", percent: percent);
        }

        protected override IReadOnlyList<(string FileName, byte[] Bytes)> OnFinish(IReadOnlyList<(string FileName, byte[] Bytes)> output)
        {
            Logger.Complete($"캐시 파일을 저장했습니다.");
            return base.OnFinish(output);
        }
    }
}
