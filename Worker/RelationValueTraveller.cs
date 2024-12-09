using ExcelTableConverter.Model;
using ExcelTableConverter.Worker.Validator;
using Newtonsoft.Json.Linq;

namespace ExcelTableConverter.Worker
{
    public class RelationValueTraveller : ParallelWorker<RawSheetData[], List<RelationValueValidationData>>
    {
        private const int CHUNK_SIZE = 250;

        private readonly HashSet<string> _files = new HashSet<string>();

        public RelationValueTraveller(Context ctx, IEnumerable<string> files) : base(ctx)
        {
            _files = files.ToHashSet();
        }

        protected override IEnumerable<RawSheetData[]> OnReady()
        {
            foreach (var g in Context.RawData.SelectMany(x => x.Value).GroupBy(x => (x.FileName, x.SheetName)))
            {
                if (_files.Contains(g.Key.FileName) == false)
                    continue;

                foreach (var chunk in g.Chunk(CHUNK_SIZE))
                    yield return chunk;
            }
        }

        protected override IEnumerable<List<RelationValueValidationData>> OnWork(RawSheetData[] rsds)
        {
            var queue = new Queue<RelationValueValidationData>();
            var buffer = new List<RelationValueValidationData>();

            foreach (var rawData in rsds)
            {
                foreach (var column in rawData.Columns)
                {
                    foreach (var value in column.RowValuePairs.Values)
                    {
                        queue.Enqueue(new RelationValueValidationData
                        {
                            Tracker = rawData,
                            Name = column.Name,
                            Type = column.Type,
                            Value = Context.Cast(column.Type, value),
                            Scope = column.Scope
                        });
                    }
                }
            }

            while (queue.TryDequeue(out var rvd))
            {
                if (Util.Type.IsRelation(rvd.Type, out var rel))
                {
                    if (Util.Value.IsNull(rvd.Value))
                        continue;

                    rvd.Type = rel;
                    buffer.Add(rvd);
                }
                else if (Util.Type.IsArray(rvd.Type, out var e))
                {
                    var array = rvd.Value as List<object>;
                    foreach (var x in array)
                    {
                        queue.Enqueue(new RelationValueValidationData
                        {
                            Tracker = rvd.Tracker,
                            Name = rvd.Name,
                            Type = e,
                            Value = x,
                            Scope = rvd.Scope,
                        });
                    }
                }
                else if (Util.Type.IsMap(rvd.Type, out var pair))
                {
                    var map = rvd.Value as Dictionary<object, object>;
                    foreach (var (k, v) in map)
                    {
                        queue.Enqueue(new RelationValueValidationData
                        {
                            Tracker = rvd.Tracker,
                            Name = rvd.Name,
                            Type = pair.Key,
                            Value = k,
                            Scope = rvd.Scope,
                        });

                        queue.Enqueue(new RelationValueValidationData
                        {
                            Tracker = rvd.Tracker,
                            Name = rvd.Name,
                            Type = pair.Value,
                            Value = v,
                            Scope = rvd.Scope,
                        });
                    }
                }
                else if (Util.Type.Nake(rvd.Type) == "dsl")
                {
                    var dsl = rvd.Value as DSL;
                    if (dsl == null)
                    {
                        if (Util.Type.IsNullable(rvd.Type))
                            continue;

                        throw new LogicException("알 수 없는 에러", rvd.Tracker);
                    }

                    var definedParams = Context.DSL[dsl.Type] as JArray;
                    for (int i = 0; i < dsl.Parameters.Count; i++)
                    {
                        var argument = dsl.Parameters[i];
                        queue.Enqueue(new RelationValueValidationData
                        {
                            Tracker = rvd.Tracker,
                            Name = rvd.Name,
                            Type = (definedParams[i] as JObject)["type"].Value<string>(),
                            Value = argument,
                            Scope = rvd.Scope,
                        });
                    }
                }
                else
                { }
            }

            yield return buffer;
        }

        protected override void OnWorked(RawSheetData[] input, List<RelationValueValidationData> output, int percent)
        {
            var tracker = input[0] as IExcelFileTrackable;
            Logger.Write($"관계타입 데이터를 순회중입니다. - {tracker.FileName}:{tracker.SheetName}", percent: percent);
        }

        protected override void OnError(RawSheetData[] input, Exception e, IExcelFileTrackable tracker = null)
        {
            base.OnError(input, e, tracker);
        }

        protected override IReadOnlyList<List<RelationValueValidationData>> OnFinish(IReadOnlyList<List<RelationValueValidationData>> output)
        {
            Logger.Complete($"관계타입 데이터를 순회했습니다.");
            return base.OnFinish(output);
        }
    }
}
