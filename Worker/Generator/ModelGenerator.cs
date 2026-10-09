using ExcelTableConverter.Configuration;
using ExcelTableConverter.Model;
using Newtonsoft.Json;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using System.Collections.Concurrent;

namespace ExcelTableConverter.Worker.Generator
{
    // Identifier is the name spelled for the output language; Name stays as written in the sheets.
    public record ClassModel(string Name, string Identifier, string Based, string BasedIdentifier, bool IsAbstract, List<object> Props);
    public record GroupModel(string Name, string Identifier, List<object> Props);

    // Renders <Language>/model.txt once per scope.
    // The base decides which tables, enums, consts, DSLs and containers go into a scope and in which order;
    // each language only builds the view model of a single item.
    public abstract class ModelGenerator
    {
        private static readonly string _templateDirectory = Path.Combine(AppContext.BaseDirectory, "Template");
        private static readonly ConcurrentDictionary<string, Template> _templates = new();

        protected Context Context { get; }
        protected AppConfiguration Config => Context.Configuration;

        protected ModelGenerator(Context ctx)
        {
            Context = ctx;
        }

        // Its name is the template directory and the output directory.
        protected abstract Language Language { get; }
        protected abstract string FileName { get; }

        // Template globals shared by every scope, such as namespaces.
        protected abstract ScriptObject Globals();
        protected abstract object Property(SchemaData column, int index);
        protected abstract List<object> EnumMembers(string name, List<KeyValuePair<string, EnumExpression>> members);
        protected abstract object Const(ConstData constData);
        protected abstract object DslParameter(DSLParameter parameter, int index);
        protected abstract object Container(string table, SchemaData pk, SchemaData gk);

        public virtual void Run()
        {
            var dir = Path.Join(Context.Output, Language.Name);
            var globals = Globals();
            var enums = Context.Completed.Enum
                .Where(x => x.Value.Count > 0)
                .OrderBy(x => x.Key)
                .Select(x => new GroupModel(x.Key, Language.Identifier(x.Key), EnumMembers(x.Key, x.Value.OrderBy(x => x.Value).ToList())))
                .ToList();
            var dsls = JsonConvert.DeserializeObject<Dictionary<string, List<DSLParameter>>>(Context.DSL.ToString())
                .OrderBy(x => x.Key)
                .Select(x => new GroupModel(x.Key, Language.Identifier(x.Key), x.Value.Select(DslParameter).ToList()))
                .ToList();

            foreach (var (scope, scopeName) in Config.DefinedScopes)
            {
                var model = new ScriptObject
                {
                    ["config"] = Config,
                    ["scope"] = scope,
                    ["enums"] = enums,
                    ["dsls"] = dsls,
                    ["consts"] = Consts(scope),
                    ["classes"] = Classes(scope),
                    ["containers"] = Containers(scope),
                    ["tables"] = Context.Completed.Schema
                        .Where(x => x.Value.Values.Any(x => AppConfiguration.ContainsScope(x.Scope, scope)))
                        .Select(x => x.Key)
                        .OrderBy(x => x)
                        .ToList(),
                };
                foreach (var (key, value) in globals)
                    model[key] = value;

                Directory.CreateDirectory(Path.Join(dir, scopeName));
                File.WriteAllText(Path.Join(dir, scopeName, FileName), Render($"{Language.Name}/model.txt", model));
            }

            Logger.Complete($"{Language.Name} 모델 코드 파일을 저장했습니다.");
        }

        private List<ClassModel> Classes(uint scope)
        {
            var baseTables = Context.Completed.Schema.BaseTables;
            var classes = new List<ClassModel>();
            foreach (var (table, schemaSet) in Context.Completed.Schema.OrderBy(x => Context.Completed.Schema.InheritanceLevel(x.Key)).ThenBy(x => x.Key))
            {
                var props = schemaSet.Values
                    .Select((column, i) => (column, i))
                    .Where(x => x.column.Inherited == false && AppConfiguration.ContainsScope(x.column.Scope, scope))
                    .Select(x => Property(x.column, x.i))
                    .ToList();
                if (props.Count == 0 && schemaSet.Based == null)
                    continue;

                var based = schemaSet.Based;
                classes.Add(new ClassModel(table, Language.Identifier(table), based, based == null ? null : Language.Identifier(based), baseTables.Contains(table), props));
            }
            return classes;
        }

        private List<GroupModel> Consts(uint scope)
        {
            var groups = new List<GroupModel>();
            foreach (var (name, constSet) in Context.Completed.Const.OrderBy(x => x.Key))
            {
                var props = constSet.Values.Where(x => AppConfiguration.ContainsScope(x.Scope, scope)).Select(Const).ToList();
                if (props.Count == 0)
                    continue;

                groups.Add(new GroupModel(name, Language.Identifier(name), props));
            }
            return groups;
        }

        private List<object> Containers(uint scope)
        {
            var containers = new List<object>();
            foreach (var (table, schemaSet) in Context.Completed.Schema.OrderBy(x => x.Key))
            {
                var columns = schemaSet.Values.Where(x => AppConfiguration.ContainsScope(x.Scope, scope)).ToList();
                if (columns.Count == 0 || schemaSet.Json != table)
                    continue;

                containers.Add(Container(table, columns.FirstOrDefault(x => x.ColumnType.PrimaryKey), columns.FirstOrDefault(x => x.ColumnType.GroupKey)));
            }
            return containers;
        }

        protected static string Render(string templateName, ScriptObject globals)
        {
            var template = _templates.GetOrAdd(templateName, name =>
            {
                var path = Path.Combine(_templateDirectory, name);
                var parsed = Template.Parse(File.ReadAllText(path), path);
                if (parsed.HasErrors)
                    throw new InvalidOperationException($"Template parsing errors in {path}:{Environment.NewLine}{string.Join(Environment.NewLine, parsed.Messages)}");

                return parsed;
            });

            var context = new TemplateContext
            {
                TemplateLoader = new TemplateLoader(),
                LoopLimit = 0,
                RecursiveLimit = 0,
            };
            context.PushGlobal(globals);
            return template.Render(context);
        }

        private sealed class TemplateLoader : ITemplateLoader
        {
            public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName) => Path.Combine(_templateDirectory, templateName);
            public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath) => File.ReadAllText(templatePath);
            public ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath) => new(File.ReadAllText(templatePath));
        }
    }
}
