using ExcelTableConverter.Model;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.CPP
{
    public class EnumCodeGenerator : ParallelWorker<string, (string Name, object Props)>
    {
        private static readonly Template _template = Template.Parse(File.ReadAllText($"Template/C++/enum.txt"));

        public string Result { get; private set; }

        public EnumCodeGenerator(Context ctx) : base(ctx)
        {
        }

        protected override IEnumerable<string> OnReady()
        {
            foreach (var enumName in Context.Result.Enum.Keys)
            {
                yield return enumName;
            }
        }

        protected override IEnumerable<(string, object)> OnWork(string enumName)
        {
            var props = Context.Result.Enum[enumName].Select(x => new 
            {
                Name = x.Key,
                Value = x.Value
            } as object).ToList();
            yield return (enumName, props);
        }

        protected override void OnWorked(string input, (string Name, object Props) output, int percent)
        {
            base.OnWorked(input, output, percent);
        }

        protected override IReadOnlyList<(string Name, object Props)> OnFinish(IReadOnlyList<(string Name, object Props)> output)
        {
            var items = output.OrderBy(x => x.Name).Select(x => new
            {
                x.Name,
                x.Props
            } as object).ToList();

            Result = _template.Render(new { Namespace = Util.CPP.Namespace.Access(Context.Config.Namespace), Items = items });
            
            return base.OnFinish(output);
        }
    }
}
