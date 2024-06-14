using ExcelTableConverter.Factory.Node;
using ExcelTableConverter.Model;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.Node
{
    public class ConstCodeGenerator : ParallelWorker<Scope, string>
    {
        private static readonly Template _template = Template.Parse(File.ReadAllText($"Template/Node/const.txt"));

        public Dictionary<Scope, string> Result { get; private set; } = new Dictionary<Scope, string>();

        public ConstCodeGenerator(Context ctx) : base(ctx)
        {
        }

        protected override IEnumerable<Scope> OnReady()
        {
            foreach (var scope in new[] { Scope.Server, Scope.Client })
            {
                yield return scope;
            }
        }

        protected override IEnumerable<string> OnWork(Scope scope)
        {
            var items = new List<object>();
            foreach (var (groupName, constSet) in Context.Result.Const.OrderBy(x => x.Key))
            {
                var props = new List<object>();
                foreach (var constData in constSet.Values.Where(x => x.Scope.HasFlag(scope)))
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

        protected override void OnWorked(Scope input, string output, int percent)
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
