using ExcelTableConverter.Factory.CPP;
using ExcelTableConverter.Model;
using ExcelTableConverter.Model.CPP;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.CPP
{
    public class BindFileGenerator : ParallelWorker<Scope, bool>
    {
        private static readonly Template _headerTemplate = Template.Parse(File.ReadAllText($"Template/C++/bind.header.txt"));
        private static readonly Template _sourceTemplate = Template.Parse(File.ReadAllText($"Template/C++/bind.source.txt"));
        private readonly string _dir;

        public BindFileGenerator(Context ctx) : base(ctx)
        {
            _dir = Path.Combine(Context.Output, Context.Config.BindingCodeFilePath);
        }

        protected override IEnumerable<Scope> OnReady()
        {
            if(Directory.Exists(_dir) == false)
                Directory.CreateDirectory(_dir);

            foreach (var file in Directory.GetFiles(_dir))
                File.Delete(file);

            foreach (var dir in Directory.GetDirectories(_dir))
                Directory.Delete(dir, true);

            foreach (var scope in new[] { Scope.Server, Scope.Client })
            {
                Directory.CreateDirectory(Path.Combine(_dir, $"{scope.ToString().ToLower()}", "include"));
                Directory.CreateDirectory(Path.Combine(_dir, $"{scope.ToString().ToLower()}", "source"));
                yield return scope;
            }
        }

        protected override IEnumerable<bool> OnWork(Scope scope)
        {
            var buffer = new List<BindingCodeGeneratorProperty>();
            foreach (var (tableName, schemaSet) in Context.Result.Schema.OrderBy(x => x.Key))
            {
                var ftdSchemaSet = schemaSet.Values.Where(x => x.Scope.HasFlag(scope)).ToList();
                if (ftdSchemaSet.Count == 0)
                    continue;

                var modelName = $"fb::model::{tableName}";

                var containerType = string.Empty;
                var genericType = string.Empty;
                var pk = ftdSchemaSet.FirstOrDefault(x => Util.Type.IsPrimaryKey(x.Type, out _));
                var gk = ftdSchemaSet.FirstOrDefault(x => Util.Type.IsGroupKey(x.Type, out _));
                if (gk != null && pk != null)
                {
                    containerType = "fb::model::kv_container";
                    genericType = $"{new TypeFactory(Context).Build(gk.Type)}, fb::model::kv_container<{new TypeFactory(Context).Build(pk.Type)}, {modelName}>";
                }
                else if (pk != null)
                {
                    containerType = "fb::model::kv_container";
                    genericType = $"{new TypeFactory(Context).Build(pk.Type)}, {modelName}";
                }
                else if (gk != null)
                {
                    containerType = "fb::model::kv_container";
                    genericType = $"{new TypeFactory(Context).Build(gk.Type)}, fb::model::array_container<{modelName}>";
                }
                else
                {
                    containerType = "fb::model::array_container";
                    genericType = modelName;
                }

                buffer.Add(new BindingCodeGeneratorProperty
                {
                    Name = tableName,
                    Type = containerType,
                    Generic = genericType
                });
            }

            File.WriteAllText(Path.Combine(_dir, $"{scope.ToString().ToLower()}", "include", "container.h"), _headerTemplate.Render(new { Scope = scope, Tables = buffer }));
            File.WriteAllText(Path.Combine(_dir, $"{scope.ToString().ToLower()}", "source", "container.cpp"), _sourceTemplate.Render(new { Scope = scope, Tables = buffer }));
            yield return true;
        }

        protected override void OnWorked(Scope input, bool output, int percent)
        {
            Logger.Write($"테이블 연결 코드 파일을 생성했습니다. - {input}", percent: percent);
            base.OnWorked(input, output, percent);
        }

        protected override IReadOnlyList<bool> OnFinish(IReadOnlyList<bool> output)
        {
            Logger.Complete("테이블 연결 코드 파일을 생성했습니다.");
            return base.OnFinish(output);
        }
    }
}
