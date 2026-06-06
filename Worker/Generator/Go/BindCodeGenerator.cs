using ExcelTableConverter.Factory.Go;
using ExcelTableConverter.Model;
using ExcelTableConverter.Util;
using Scriban;

namespace ExcelTableConverter.Worker.Generator.Go
{
    public class BindCodeGenerator : ParallelWorker<Scope, KeyValuePair<Scope, string>>
    {
        private static readonly Template _template = Template.Parse(File.ReadAllText($"Template/Go/container.txt"));
        private readonly HashSet<string> _baseTableNames;

        public Dictionary<Scope, string> Result { get; private set; } = new Dictionary<Scope, string>();

        public BindCodeGenerator(Context ctx) : base(ctx)
        {
            _baseTableNames = ctx.Completed.Schema.FindBaseTables();
        }

        protected override IEnumerable<Scope> OnReady()
        {
            foreach (var scope in new[] { Scope.Server, Scope.Client })
            {
                yield return scope;
            }
        }

        protected override IEnumerable<KeyValuePair<Scope, string>> OnWork(Scope scope)
        {
            var opt = new Factory.DataFormatOption();
            opt.Add("hook", "container.Hook");

            var elementType = string.Empty;
            var buffer = new List<object>();
            foreach (var (tableName, schemaSet) in Context.Completed.Schema.OrderBy(x => x.Key))
            {
                var ftdSchemaSet = schemaSet.Values.Where(x => x.Scope.HasFlag(scope)).ToList();
                if (ftdSchemaSet.Count == 0)
                    continue;

                var upperCamelTableName = ScribanEx.UpperCamel(tableName);
                var interfaceTableName = upperCamelTableName + "Interface";

                var pk = ftdSchemaSet.FirstOrDefault(x => Util.Type.IsPrimaryKey(x.Type, out _));
                var gk = ftdSchemaSet.FirstOrDefault(x => Util.Type.IsGroupKey(x.Type, out _));
                var isAbstract = _baseTableNames.Contains(tableName);
                var buildFuncBody = string.Empty;
                var returnType = string.Empty;

                if (gk != null && pk != null)
                {
                    var kt1 = new TypeFactory(Context).Build(gk.Type);
                    var kt2 = new TypeFactory(Context).Build(pk.Type);
                    var vt = upperCamelTableName;
                    elementType = isAbstract ? $"{vt}Interface" : vt;
                    var valueBuilderBody = string.Empty;
                    if (isAbstract)
                    {
                        valueBuilderBody = $@"            based, err := New{vt}Builder(nil).Build(data)
            if err != nil {{
               return nil, err
            }}

            if container.Hook != nil {{
               return container.Hook(&based, data)
            }}

            return &based, nil";
                    }
                    else
                    {
                        valueBuilderBody = $@"            return New{vt}Builder(container.Hook).Build(data)";
                    }

                    buildFuncBody = $@"   innerMapBuilder := func(data json.RawMessage) (map[{kt2}]{vt}, error) {{
      innerDictBuilder := NewDictionaryBuilder(
         func(data json.RawMessage) ({kt2}, error) {{
            return New{new TypeBuilderFactory(Context).Build(kt2)}.Build(data)
         }},
         func(data json.RawMessage) ({elementType}, error) {{
{valueBuilderBody}
         }},
      )
      return innerDictBuilder.Build(data)
   }}

   outerDictBuilder := NewDictionaryBuilder(
      func(data json.RawMessage) ({kt1}, error) {{
         return New{new TypeBuilderFactory(Context).Build(kt1)}.Build(data)
      }},
      innerMapBuilder,
   )

   return outerDictBuilder.Build(data)";


                    returnType = $"map[{kt1}]map[{kt2}]{elementType}";
                }
                else if (pk != null)
                {
                    var kt = new TypeFactory(Context).Build(Context.Completed.Schema.GetRootTableType(pk.Type));
                    var vt = upperCamelTableName;
                    var valueBuilderBody = string.Empty;
                    elementType = isAbstract ? $"{vt}Interface" : vt;
                    if (isAbstract)
                    {
                        valueBuilderBody = $@"         based, err := New{vt}Builder(nil).Build(data)
         if err != nil {{
            return nil, err
         }}

         if container.Hook != nil {{
            return container.Hook(&based, data)
         }}

         return &based, nil";
                    }
                    else
                    {
                        valueBuilderBody = $@"         return New{vt}Builder(container.Hook).Build(data)";
                    }

                    buildFuncBody = $@"   dictBuilder := NewDictionaryBuilder(
      func(data json.RawMessage) ({kt}, error) {{
         return New{new TypeBuilderFactory(Context).Build(kt)}.Build(data)
      }},
      func(data json.RawMessage) ({elementType}, error) {{
{valueBuilderBody}
      }},
   )
   return dictBuilder.Build(data)";

                    returnType = $"map[{kt}]{elementType}";
                }
                else if (gk != null)
                {
                    var kt = new TypeFactory(Context).Build(gk.Type);
                    var vt = upperCamelTableName;
                    var valueBuilderBody = string.Empty;
                    elementType = isAbstract ? $"{vt}Interface" : vt;
                    if (isAbstract)
                    {
                        valueBuilderBody = $@"      based, err := New{vt}Builder(nil).Build(data)
      if err != nil {{
         return nil, err
      }}

      if container.Hook != nil {{
         return container.Hook(&based, data)
      }}

      return &based, nil";
                    }
                    else
                    {
                        valueBuilderBody = $@"      return New{vt}Builder(container.Hook).Build(data)";
                    }

                    buildFuncBody = $@"   arrayBuilder := NewArrayBuilder(func(data json.RawMessage) ({elementType}, error) {{
{valueBuilderBody}
   }})
   dictBuilder := NewDictionaryBuilder(
      func(data json.RawMessage) ({kt}, error) {{
         return New{new TypeBuilderFactory(Context).Build(kt)}.Build(data)
      }},
      func(data json.RawMessage) ([]{elementType}, error) {{
         return arrayBuilder.Build(data)
      }},
   )
   return dictBuilder.Build(data)";

                    returnType = $"map[{kt}][]{elementType}";
                }
                else
                {
                    var vt = upperCamelTableName;
                    var valueBuilderBody = string.Empty;
                    elementType = isAbstract ? $"{vt}Interface" : vt;
                    if (isAbstract)
                    {
                        valueBuilderBody = $@"      based, err := New{vt}Builder(nil).Build(data)
      if err != nil {{
         return nil, err
      }}

      if container.Hook != nil {{
         return container.Hook(&based, data)
      }}

      return &based, nil";
                    }
                    else
                    {
                        valueBuilderBody = $@"      return New{vt}Builder(container.Hook).Build(data)";
                    }

                    buildFuncBody = $@"   arrayBuilder := NewArrayBuilder(func(data json.RawMessage) ({elementType}, error) {{
{valueBuilderBody}
   }})
   return arrayBuilder.Build(data)";

                    returnType = $"[]{elementType}";
                }

                var buildFunc = $@"func (container *{upperCamelTableName}ContainerBuilder) Build(data json.RawMessage) ({returnType}, error) {{
{buildFuncBody}
}}";

                buffer.Add(new
                {
                    Name = tableName,
                    Based = Context.Completed.Schema[tableName].Based,
                    Json = Context.Completed.Schema[tableName].Json,
                    IsAbstract = isAbstract,
                    BuildFunc = buildFunc,
                    ReturnType = returnType,
                    ElementType = elementType,
                });
            }

            var obj = new ScribanEx
            {
                ["scope"] = scope,
                ["tables"] = buffer,
                ["config"] = Context.Configuration,
                ["base_tables"] = Context.Completed.Schema.FindBaseTables(),
            };

            var ctx = ScribanEx.CreateContext();
            ctx.PushGlobal(obj);
            var code = _template.Render(ctx);
            yield return new KeyValuePair<Scope, string>(scope, code);
        }

        protected override void OnWorked(Scope input, KeyValuePair<Scope, string> output, int percent)
        {
            Result.Add(output.Key, output.Value);
            base.OnWorked(input, output, percent);
        }

        protected override IReadOnlyList<KeyValuePair<Scope, string>> OnFinish(IReadOnlyList<KeyValuePair<Scope, string>> output)
        {
            return base.OnFinish(output);
        }
    }
}
