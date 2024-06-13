using ExcelTableConverter.Factory.CPP;
using ExcelTableConverter.Model;
using ExcelTableConverter.Model.CPP;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.CPP
{
    public class ConstCodeGenerator : ParallelWorker<Scope, string>
    {
        private static readonly Template _template = Template.Parse(File.ReadAllText($"Template/C++/const.txt"));
        private readonly string _dir;

        public Dictionary<Scope, string> Result { get; private set; } = new Dictionary<Scope, string>();

        public ConstCodeGenerator(Context ctx) : base(ctx)
        {
            _dir = Path.Combine(ctx.Output, Context.Config.ConstCodeFilePath);
            if (Directory.Exists(_dir) == false)
                Directory.CreateDirectory(_dir);

            foreach (var dir in Directory.GetDirectories(_dir))
                Directory.Delete(dir, true);

            foreach (var file in Directory.GetFiles(_dir))
                File.Delete(file);
        }

        protected override IEnumerable<Scope> OnReady()
        {
            foreach (var scope in new[] { Scope.Server, Scope.Client })
            {
                var dir = Path.Combine(_dir, $"{scope}".ToLower());
                if (Directory.Exists(dir) == false)
                    Directory.CreateDirectory(dir);

                foreach (var file in Directory.GetFiles(dir))
                    File.Delete(file);

                yield return scope;
            }
        }

        protected override IEnumerable<string> OnWork(Scope scope)
        {
            var items = new Dictionary<string, List<ConstCodeGeneratorProperty>>();
            var scopes = new[] { scope, Scope.Common };
            foreach (var (groupName, constSet) in Context.Result.Const.OrderBy(x => x.Key))
            {
                var properties = new List<ConstCodeGeneratorProperty>();
                foreach (var constData in constSet.Values.Where(x => scopes.Contains(x.Scope)))
                {
                    properties.Add(new ConstCodeGeneratorProperty
                    {
                        Name = constData.Name,
                        Type = new TypeFactory(Context).Build(constData.Type),
                        Value = new AllocateValueFactory(Context).Build(constData.Type, constData.Value),
                    });
                }

                items.Add(groupName, properties);
            }

            yield return _template.Render(new { Namespace = Util.CPP.Namespace.Access(Context.Config.Namespace), Super = scope == Scope.Common, Scope = scope, Items = items });
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
