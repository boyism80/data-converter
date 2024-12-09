using ExcelTableConverter.Factory.CS;
using ExcelTableConverter.Model;
using ExcelTableConverter.Util;
using Scriban;
using System;

namespace ExcelTableConverter.Worker.Generator.CS
{
    public class ConstCodeGenerator : ParallelWorker<Scope, string>
    {
        private static readonly Template _template = Template.Parse(File.ReadAllText($"Template/C#/const.txt"));

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
            var items = new Dictionary<string, List<object>>();
            foreach (var (groupName, constSet) in Context.Result.Const.OrderBy(x => x.Key))
            {
                var props = new List<object>();
                foreach (var constData in constSet.Values.Where(x => x.Scope.HasFlag(scope)))
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

            var obj = new ScribanEx();
            obj.Add("super", scope == Scope.Common);
            obj.Add("scope", scope);
            obj.Add("items", items);
            obj.Add("config", Context.Config);

            var ctx = new TemplateContext();
            ctx.PushGlobal(obj);

            yield return _template.Render(ctx);
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
