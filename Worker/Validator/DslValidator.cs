using ExcelTableConverter.Model;
using Newtonsoft.Json.Linq;

namespace ExcelTableConverter.Worker.Validator
{
    public class DslValidator : ParallelWorker<KeyValuePair<string, JArray>, bool>
    {
        public DslValidator(Context ctx) : base(ctx)
        {
        }

        private static string GetDSLFormat(KeyValuePair<string, JArray> dsl)
        {
            var header = dsl.Key;
            var parameters = dsl.Value;
            var paramNameTypePairs = parameters.Select(x =>
            {
                var name = x["name"].Value<string>();
                var type = x["type"].Value<string>();
                return $"{name}:{type}";
            }).ToList();
            return $"{header}({string.Join(", ", paramNameTypePairs)})";
        }

        protected override IEnumerable<KeyValuePair<string, JArray>> OnReady()
        {
            foreach (var pair in Context.DSL)
            {
                yield return new KeyValuePair<string, JArray>(pair.Key, pair.Value as JArray);
            }
        }

        protected override IEnumerable<bool> OnWork(KeyValuePair<string, JArray> dsl)
        {
            var header = dsl.Key;
            var parameters = dsl.Value.Cast<JObject>().ToList();
            var format = GetDSLFormat(dsl);
            for (int i = 0; i < parameters.Count; i++)
            {
                var param = dsl.Value[i] as JObject;
                var name = param["name"].Value<string>();
                var type = param["type"].Value<string>();
                if (Util.Type.IsRelation(type, out var rel))
                {
                    rel = Util.Type.Nake(rel);
                    if (Context.AllTableNames.Contains(rel) == false)
                        throw new LogicException($"'{format}'에 정의된 {i + 1}번째 인자 '{name}'의 타입 '{rel}'은 존재하지 않는 테이블입니다.");

                    if (Context.KeyTableNames.Contains(rel) == false)
                        throw new LogicException($"'{format}'에 정의된 {i + 1}번째 인자 '{name}'의 타입 '{rel}'은 키가 정의되지 않는 테이블입니다.");
                }
            }

            var optionalParam = parameters.FirstOrDefault(x => x.ContainsKey("default"));
            if (optionalParam != null)
            {
                var optionalParamIndex = parameters.IndexOf(optionalParam);
                for (int i = optionalParamIndex + 1; i < parameters.Count; i++)
                {
                    var isEssential = parameters[i].ContainsKey("default") == false;
                    if (isEssential)
                    {
                        var optionalParamName = optionalParam["name"].Value<string>();
                        var essentialParamName = parameters[i]["name"].Value<string>();
                        throw new LogicException($"'{format}'에 정의된 {i + 1}번째 인자 '{essentialParamName}'은 디폴트 인자 '{optionalParamName}' 뒤에 올 수 없습니다.");
                    }
                }
            }
            yield return true;
        }

        protected override void OnWorked(KeyValuePair<string, JArray> input, bool output, int percent)
        {
            Logger.Write($"DSL 정의문을 검사했습니다. - {input.Key}", percent: percent);
            base.OnWorked(input, output, percent);
        }

        protected override IReadOnlyList<bool> OnFinish(IReadOnlyList<bool> output)
        {
            Logger.Complete("DSL 정의문을 검사했습니다");
            return base.OnFinish(output);
        }
    }
}
