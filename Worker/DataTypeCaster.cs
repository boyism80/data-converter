using ExcelTableConverter.Model;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Text;

namespace ExcelTableConverter.Worker
{
    public class CastTypeChunkData
    {
        public SourceSheetData Tracker { get; set; }
        public SourceColumns Columns { get; set; }
        public string Json { get; set; }
    }

    public class DataConvertResult : IExcelFileTrackable
    {
        public string FileName { get; set; }
        public string SheetName { get; set; }
        public string TableName { get; set; }
        public List<Dictionary<string, DataValue>> Rows { get; set; }
        public string Json { get; set; }
    }

    public class DataTypeCaster : ParallelWorker<CastTypeChunkData, DataConvertResult>
    {
        private const int CHUNK_SIZE = 250;

        private int _runtimeAdditionalCount = 0;
        private readonly HashSet<string> _loadedCacheFiles = new HashSet<string>();

        public DataTypeCaster(Context ctx) : base(ctx)
        {
        }

        protected override IEnumerable<CastTypeChunkData> OnReady()
        {
            foreach (var sheetData in Context.Source.Data.SelectMany(x => x.Value))
            {
                var chunks = sheetData.Chunk(CHUNK_SIZE).ToList();
                if (chunks.Count == 0)
                {
                    yield return new CastTypeChunkData
                    {
                        Tracker = sheetData,
                        Columns = new SourceColumns(),
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
                bool first;
                lock (_loadedCacheFiles)
                    first = _loadedCacheFiles.Add(cacheFilePath);

                if (first)
                {
                    using var stream = new GZipStream(File.OpenRead(cacheFilePath), CompressionMode.Decompress);
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    foreach (var data in JsonConvert.DeserializeObject<Dictionary<string, List<DataConvertResult>>>(reader.ReadToEnd(), Context.CACHE_JSON).SelectMany(x => x.Value))
                        yield return data;
                }

                yield break;
            }

            var errors = new List<Exception>();
            var (boldColumns, normalColumns) = chunkData.Columns.Split();
            var boldKeyColumns = boldColumns?.FirstOrDefault(x => x.ColumnType.Key);
            if (boldColumns != null)
            {
                Interlocked.Add(ref _runtimeAdditionalCount, 1);

                var boldColumnSet = boldColumns.ToDictionary(x => x.Name);
                var table = string.Format(Context.Configuration.ParentTableFormat, chunkData.Tracker.TableName);
                var models = boldColumns.Rows();
                var dataSet = new List<Dictionary<string, DataValue>>();
                for (int row = 0; row < models.Count; row++)
                {
                    var model = models[row];
                    var values = new Dictionary<string, DataValue>();
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
                var table = chunkData.Tracker.TableName;
                var models = normalColumns.Rows();
                var dataSet = new List<Dictionary<string, DataValue>>();

                for (int row = 0; row < models.Count; row++)
                {
                    var model = models[row];
                    var values = new Dictionary<string, DataValue>();
                    foreach (var (k, v) in model)
                    {
                        try
                        {
                            values.Add(k, Context.Cast(normalColumnSet[k].Type, v, chunkData.Tracker));
                        }
                        catch (LogicException e)
                        {
                            errors.Add(new LogicException(e.Message, chunkData.Tracker));
                        }
                        catch (Exception e)
                        {
                            errors.Add(e);
                        }
                    }

                    if (boldColumns != null)
                    {
                        try
                        {
                            var parentOffset = normalColumns.Select(normalColumn => normalColumn.RowValuePairs.Keys.Cast<int?>().ElementAtOrDefault(row)).Where(x => x != null).OrderBy(x => x).FirstOrDefault().Value;

                            var parentRows = boldKeyColumns.RowValuePairs.Where(x => x.Key < parentOffset).OrderByDescending(x => x.Key).ToArray();
                            if (parentRows.Length == 0)
                                throw new LogicException($"부모 컬럼에 문제가 있습니다. {parentOffset} 라인을 확인하세요.", chunkData.Tracker);
                            var parent = parentRows.First().Value;
                            values.Add(Context.Configuration.ParentPropName, Context.Cast(boldKeyColumns.Type, parent));
                        }
                        catch (LogicException e)
                        {
                            errors.Add(new LogicException(e.Message, chunkData.Tracker));
                        }
                        catch (Exception e)
                        {
                            errors.Add(e);
                        }
                    }

                    if (values.Count == 0)
                        continue;

                    dataSet.Add(values);
                }

                if (errors.Count > 0)
                    throw new AggregateException(errors);

                yield return new DataConvertResult
                {
                    FileName = chunkData.Tracker.FileName,
                    SheetName = chunkData.Tracker.SheetName,
                    TableName = chunkData.Tracker.TableName,
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
                    TableName = chunkData.Tracker.TableName,
                    Rows = new List<Dictionary<string, DataValue>>(),
                    Json = chunkData.Json
                };
            }
        }

        protected override void OnWorked(CastTypeChunkData input, DataConvertResult output, int percent)
        {
            Logger.Write($"테이블 데이터를 변환했습니다. - {input.Tracker.Root}");
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
