using ExcelTableConverter.Model;
using ExcelTableConverter.Worker.Validator;
using Newtonsoft.Json.Linq;

namespace ExcelTableConverter.Worker
{
    // Consts are always visited: any data change can break a const relation, and every const is cast again on each run.
    public class RelationValueTraveller : ParallelWorker<IExcelFileTrackable[], List<RelationValueValidationData>>
    {
        private const int CHUNK_SIZE = 250;

        private readonly HashSet<string> _files = new HashSet<string>();

        public RelationValueTraveller(Context ctx, IEnumerable<string> files) : base(ctx)
        {
            _files = files.ToHashSet();
        }

        protected override IEnumerable<IExcelFileTrackable[]> OnReady()
        {
            foreach (var g in Context.Source.Data.SelectMany(x => x.Value).GroupBy(x => (x.FileName, x.SheetName)))
            {
                if (_files.Contains(g.Key.FileName) == false)
                    continue;

                foreach (var chunk in g.Chunk(CHUNK_SIZE))
                    yield return chunk;
            }

            foreach (var g in Context.Source.Const.SelectMany(x => x.Value).GroupBy(x => (x.FileName, x.SheetName)))
            {
                foreach (var chunk in g.Chunk(CHUNK_SIZE))
                    yield return chunk;
            }
        }

        protected override IEnumerable<List<RelationValueValidationData>> OnWork(IExcelFileTrackable[] trackers)
        {
            var queue = new Queue<RelationValueValidationData>();
            var buffer = new List<RelationValueValidationData>();

            foreach (var tracker in trackers)
            {
                if (tracker is SourceSheetData sourceData)
                {
                    foreach (var column in sourceData.Columns)
                    {
                        foreach (var value in column.RowValuePairs.Values)
                        {
                            queue.Enqueue(new RelationValueValidationData
                            {
                                Tracker = sourceData,
                                Name = column.Name,
                                Type = column.Type,
                                Value = Context.Cast(column.Type, value),
                                Scope = column.Scope
                            });
                        }
                    }
                }
                else if (tracker is SourceConst sourceConst)
                {
                    queue.Enqueue(new RelationValueValidationData
                    {
                        Tracker = sourceConst,
                        Name = sourceConst.Name,
                        Type = sourceConst.Type,
                        Value = Context.Cast(sourceConst.Type, sourceConst.Value),
                        Scope = sourceConst.Scope
                    });
                }
            }

            while (queue.TryDequeue(out var rvd))
            {
                var columnType = ColumnType.Parse(rvd.Type);
                if (columnType.Relation != null)
                {
                    if (rvd.Value is NullValue)
                        continue;

                    buffer.Add(rvd);
                }
                else if (DataType.IsArray(rvd.Type, out var e))
                {
                    foreach (var x in ((ArrayValue)rvd.Value).Items)
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
                else if (DataType.IsMap(rvd.Type, out var mapKey, out var mapValue))
                {
                    foreach (var (k, v) in ((MapValue)rvd.Value).Entries)
                    {
                        queue.Enqueue(new RelationValueValidationData
                        {
                            Tracker = rvd.Tracker,
                            Name = rvd.Name,
                            Type = mapKey,
                            Value = k,
                            Scope = rvd.Scope,
                        });

                        queue.Enqueue(new RelationValueValidationData
                        {
                            Tracker = rvd.Tracker,
                            Name = rvd.Name,
                            Type = mapValue,
                            Value = v,
                            Scope = rvd.Scope,
                        });
                    }
                }
                else if (columnType.Naked == "dsl")
                {
                    if (rvd.Value is not DslValue dsl)
                    {
                        if (columnType.Nullable)
                            continue;

                        throw new LogicException("알 수 없는 에러", rvd.Tracker);
                    }

                    var definedParams = Context.DSL[dsl.Header] as JArray;
                    for (int i = 0; i < dsl.Params.Count; i++)
                    {
                        var argument = dsl.Params[i];
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

        protected override void OnWorked(IExcelFileTrackable[] input, List<RelationValueValidationData> output, int percent)
        {
            var tracker = input[0];
            Logger.Write($"관계타입 데이터를 순회중입니다. - {tracker.FileName}:{tracker.SheetName}");
        }

        protected override void OnError(IExcelFileTrackable[] input, Exception e, IExcelFileTrackable tracker = null)
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
