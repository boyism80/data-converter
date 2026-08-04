using ExcelTableConverter.Configuration;
using ExcelTableConverter.Factory.Node;
using ExcelTableConverter.Model;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.Node
{
    public class ConstCodeGenerator : ParallelWorker<uint, string>
    {
        private static readonly Template _template = Template.Parse(File.ReadAllText($"Template/Node/const.txt"));

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
            var items = new List<object>();
            foreach (var (groupName, constSet) in Context.Completed.Const.OrderBy(x => x.Key))
            {
                var props = new List<object>();
                foreach (var constData in constSet.Values.Where(x => AppConfiguration.ContainsScope(x.Scope, scope)))
                {
                    props.Add(new
                    {
                        Name = constData.Name,
                        Value = new AllocateValueFactory(Context).Build(constData.Type, constData.Value),
                    });
                }

                if (props.Count == 0)
                    continue;

                items.Add(new
                {
                    Name = groupName,
                    Props = props,
                });
            }

            yield return _template.Render(new { Items = items });
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
