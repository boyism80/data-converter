using ExcelTableConverter.Configuration;
using ExcelTableConverter.Factory.CS;
using ExcelTableConverter.Model;
using ExcelTableConverter.Util;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.CS
{
    public class ConstCodeGenerator : ParallelWorker<uint, string>
    {
        private static readonly Template _template = Template.Parse(File.ReadAllText($"Template/C#/const.txt"));

        public Dictionary<uint, string> Result { get; private set; } = new Dictionary<uint, string>();

        public ConstCodeGenerator(Context ctx) : base(ctx)
        {
        }

        protected override IEnumerable<uint> OnReady()
        {
            foreach (var (scope, _) in Context.Configuration.DefinedScopes)
            {
                yield return scope;
            }
        }

        protected override IEnumerable<string> OnWork(uint scope)
        {
            var items = new Dictionary<string, List<object>>();
            foreach (var (groupName, constSet) in Context.Completed.Const.OrderBy(x => x.Key))
            {
                var props = new List<object>();
                foreach (var constData in constSet.Values.Where(x => AppConfiguration.ContainsScope(x.Scope, scope)))
                {
                    props.Add(new
                    {
                        Name = constData.Name,
                        Type = new TypeFactory(Context).Build(constData.Type),
                        Value = new AllocateValueFactory(Context).Build(constData.Type, constData.Value),
                    });
                }

                if (props.Count == 0)
                    continue;

                items.Add(groupName, props);
            }

            var obj = new ScribanEx
            {
                ["scope"] = scope,
                ["items"] = items,
                ["config"] = Context.Configuration,
            };

            var ctx = ScribanEx.CreateContext();
            ctx.PushGlobal(obj);

            yield return _template.Render(ctx);
        }

        protected override void OnWorked(uint input, string output, int percent)
        {
            Result.Add(input, output);
            base.OnWorked(input, output, percent);
        }

        protected override IReadOnlyList<string> OnFinish(IReadOnlyList<string> output)
        {
            return base.OnFinish(output);
        }
    }
}
