using ExcelTableConverter.Model;
using ExcelTableConverter.Util;
using System.Data;

namespace ExcelTableConverter.Worker
{
    public class CastTypeChunkData
    { 
        public IExcelFileTrackable Tracker { get; set; }
        public List<RawDataColumns> Columns { get; set; }
        public string Json { get; set; }
    }

    public class DataConvertResult : IExcelFileTrackable
    {
        public string FileName { get; set; }
        public string SheetName { get; set; }
        public string TableName { get; set; }
        public List<Dictionary<string, object>> Rows {get;set;}
        public string Json { get; set; }
    }

    public class DataTypeCaster : ParallelWorker<CastTypeChunkData, DataConvertResult>
    {
        private const int CHUNK_SIZE = 250;

        private int _runtimeAdditionalCount = 0;
        private readonly HashSet<string> _passedCacheFiles = new HashSet<string>();
        private readonly Mutex _mutex = new Mutex();

        public DataTypeCaster(Context ctx) : base(ctx)
        {
        }

        private bool IsCachFileePassed(string fileName)
        {
            _mutex.WaitOne();
            var result = _passedCacheFiles.Contains(fileName);
            if (!result)
                _passedCacheFiles.Add(fileName);
            _mutex.ReleaseMutex();

            return result;
        }

        protected override IEnumerable<CastTypeChunkData> OnReady()
        {
            foreach (var sheetData in Context.RawData.SelectMany(x => x.Value))
            {
                var chunks = sheetData.Chunk(CHUNK_SIZE).ToList();
                if (chunks.Count == 0)
                {
                    yield return new CastTypeChunkData
                    {
                        Tracker = sheetData,
                        Columns = new List<RawDataColumns>(),
                        Json = sheetData.Json
                    };
                }
                else
                {
                    foreach (var columns in chunks)
                    {
                        yield return new CastTypeChunkData
                        {
                            Tracker = sheetData,
                            Columns = columns,
                            Json = sheetData.Json
                        };
                    }
                }
            }
        }

        protected override IEnumerable<DataConvertResult> OnWork(CastTypeChunkData chunkData)
        {
            var cacheFilePath = Context.GetCacheFilePath(chunkData.Tracker.FileName);
            if (File.Exists(cacheFilePath))
            {
                if (IsCachFileePassed(cacheFilePath) == false)
                {
                    foreach (var data in ZipUtil.Unzip<Dictionary<string, List<DataConvertResult>>>(File.ReadAllBytes(cacheFilePath)).SelectMany(x => x.Value))
                        yield return data;
                }

                yield break;
            }

            var errors = new List<Exception>();
            var (boldColumns, normalColumns) = chunkData.Columns.Split();
            var boldKeyColumns = boldColumns?.FirstOrDefault(x => Util.Type.IsKey(x.Type, out _));
            if (boldColumns != null)
            {
                Interlocked.Add(ref _runtimeAdditionalCount, 1);

                var boldColumnSet = boldColumns.ToDictionary(x => x.Name);
                var table = string.Format(Context.Config.ParentTableFormat, chunkData.Tracker.GetTableName());
                var models = boldColumns.ToModels();
                var dataSet = new List<Dictionary<string, object>>();
                for (int row = 0; row < models.Count; row++)
                {
                    var model = models[row];
                    var values = new Dictionary<string, object>();
                    foreach (var (k, v) in model)
                    {
                        try
                        {
                            values.Add(k, Context.Cast(boldColumnSet[k].Type, v));
                        }
                        catch (Exception e)
                        {
                            errors.Add(e);
                        }
                    }

                    dataSet.Add(values);
                }

                if (errors.Count > 0)
                    throw new AggregateException(errors);

                yield return new DataConvertResult
                { 
                    FileName = chunkData.Tracker.FileName,
                    SheetName = chunkData.Tracker.SheetName,
                    TableName = table,
                    Rows = dataSet,
                    Json = chunkData.Json
                };
            }

            if (normalColumns != null)
            {
                var normalColumnSet = normalColumns.ToDictionary(x => x.Name);
                var table = chunkData.Tracker.GetTableName();
                var models = normalColumns.ToModels();
                var dataSet = new List<Dictionary<string, object>>();

                for (int row = 0; row < models.Count; row++)
                {
                    var model = models[row];
                    var values = new Dictionary<string, object>();
                    foreach (var (k, v) in model)
                    {
                        try
                        {
                            values.Add(k, Context.Cast(normalColumnSet[k].Type, v));
                        }
                        catch (Exception e)
                        {
                            errors.Add(e);
                        }
                    }

                    if (boldColumns != null)
                    {
                        var parentRow = normalColumns.Select(normalColumn => normalColumn.RowValuePairs.Keys.Cast<int?>().ElementAtOrDefault(row)).Where(x => x != null).OrderBy(x => x).FirstOrDefault().Value;
                        var parent = boldKeyColumns.RowValuePairs.Where(x => x.Key < parentRow).OrderByDescending(x => x.Key).First().Value;
                        values.Add(Context.Config.ParentPropName, Context.Cast(boldKeyColumns.Type, parent));
                    }

                    if (values.Count == 0)
                        continue;

                    dataSet.Add(values);
                }

                if (errors.Count > 0)
                    throw new AggregateException(errors);

                if (dataSet.Count == 0)
                    dataSet = new List<Dictionary<string, object>>();

                yield return new DataConvertResult
                {
                    FileName = chunkData.Tracker.FileName,
                    SheetName = chunkData.Tracker.SheetName,
                    TableName = chunkData.Tracker.GetTableName(),
                    Rows = dataSet,
                    Json = chunkData.Json
                };
            }
            else
            {
                yield return new DataConvertResult
                {
                    FileName = chunkData.Tracker.FileName,
                    SheetName = chunkData.Tracker.SheetName,
                    TableName = chunkData.Tracker.GetTableName(),
                    Rows = new List<Dictionary<string, object>>(),
                    Json = chunkData.Json
                };
            }
        }

        protected override void OnWorked(CastTypeChunkData input, DataConvertResult output, int percent)
        {
            Logger.Write($"테이블 데이터를 변환했습니다. - {input.Tracker.GetRootName()}", percent: percent);
        }

        protected override int RuntimeAdditionalCount()
        {
            return _runtimeAdditionalCount;
        }

        protected override void OnError(CastTypeChunkData input, Exception e, IExcelFileTrackable tracker = null)
        {
            base.OnError(input, e, input.Tracker);
        }

        protected override IReadOnlyList<DataConvertResult> OnFinish(IReadOnlyList<DataConvertResult> output)
        {
            Logger.Complete("데이터 변환을 완료했습니다.");
            return output.Where(x => x.Rows != null).ToList();
        }
    }
}
