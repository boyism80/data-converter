using ExcelTableConverter.Model;
using ExcelTableConverter.Util;
using Newtonsoft.Json.Linq;

namespace ExcelTableConverter.Worker.Validator
{
    public class EnumValidator : ParallelWorker<RawEnum, bool>
    {
        private readonly Dictionary<string, Dictionary<string, List<object>>> _merge = new Dictionary<string, Dictionary<string, List<object>>>();

        public EnumValidator(Context ctx) : base(ctx)
        {
            
        }

        private void Assert(IExcelFileTrackable tracker, List<object> array)
        {
            var last = array.LastOrDefault();
            if (last == null)
                throw new LogicException("구문이 올바르지 않습니다.", tracker);

            if (last is string lasts && Util.Enum.Parse(lasts).Groups["value"].Success == false)
                throw new LogicException("구문이 올바르지 않습니다.", tracker);

            for (int i = 0; i < array.Count; i++)
            {
                var prev = array.ElementAtOrDefault(i - 1);
                var curr = array[i];

                if (curr is JArray jarray)
                {
                    Assert(tracker, jarray.Select(x => x as object).ToList());
                }
                else if (curr is List<object> list)
                {
                    if (prev != null)
                    {
                        if (prev is List<object>)
                            throw new LogicException("구문이 올바르지 않습니다.", tracker);

                        if (prev is string prevs && Util.Enum.Parse(prevs).Groups["value"].Success)
                            throw new LogicException("구문이 올바르지 않습니다.", tracker);
                    }

                    Assert(tracker, list);
                }
                else
                {
                    var str = curr is JValue jvalue ? jvalue.Value as string : curr as string;
                    var matched = Util.Enum.Parse(str);
                    if (matched.Groups["value"].Success)
                    {
                        var value = matched.Groups["value"].Value;
                        var isHex = false;
                        try { Convert.ToUInt32(value, 16); isHex = true; } catch { }

                        if (!isHex && int.TryParse(value, out _) == false)
                        {
                            var table = tracker.GetTableName();
                            if (_merge[table].ContainsKey(value) == false)
                                throw new LogicException($"{value}는 {table}에 존재하지 않는 열거형입니다.", tracker);
                        }

                        if (prev != null)
                        {
                            if (prev is List<object>)
                                throw new LogicException("구문이 올바르지 않습니다.", tracker);

                            if (prev is string s && Util.Enum.Parse(s).Groups["value"].Success)
                                throw new LogicException("구문이 올바르지 않습니다.", tracker);
                        }
                    }
                    else if (matched.Groups["op"].Success)
                    {
                        if (prev == null)
                            throw new LogicException("구문이 올바르지 않습니다.", tracker);

                        if (prev is string s && Util.Enum.Parse(s).Groups["op"].Success)
                            throw new LogicException("구문이 올바르지 않습니다.", tracker);
                    }
                    else if (matched.Groups["inv"].Success)
                    {
                        if (prev != null && prev is string s && Util.Enum.Parse(s).Groups["inv"].Success)
                            throw new LogicException("구문이 올바르지 않습니다.", tracker);
                    }
                }
            }
        }

        protected override IEnumerable<RawEnum> OnReady()
        {
            foreach (var g in Context.RawEnum.SelectMany(x => x.Value).GroupBy(x => x.Table))
            {
                var table = g.Key;
                var merge = new Dictionary<string, List<object>>();
                foreach (var x in g.Select(x => x.Values))
                {
                    foreach (var (k, v) in x)
                        merge.Add(k, v);
                }

                _merge.Add(table, merge);
            }

            foreach (var raws in Context.RawEnum.Values)
            {
                foreach(var raw in raws)
                    yield return raw;
            }
        }

        protected override IEnumerable<bool> OnWork(RawEnum value)
        {
            foreach (var (k, v) in value.Values)
            {
                Assert(value, v);
            }

            yield return true;
        }

        protected override void OnWorked(RawEnum input, bool output, int percent)
        {
            Logger.Write("열거형 구문을 검사중입니다.");
            base.OnWorked(input, output, percent);
        }

        protected override IReadOnlyList<bool> OnFinish(IReadOnlyList<bool> output)
        {
            Logger.Complete("열거형 구문을 검사했습니다.");
            return base.OnFinish(output);
        }
    }
}
