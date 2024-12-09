using ExcelTableConverter.Factory;
using ExcelTableConverter.Util;
using ExcelTableConverter.Worker;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Reflection;

namespace ExcelTableConverter.Model
{
    using ConstContainer = Dictionary<string, Dictionary<string, ConstData>>;
    using DataContainer = Dictionary<string, Dictionary<string, List<DataConvertResult>>>;
    using EnumContainer = Dictionary<string, Dictionary<string, List<object>>>;
    using RawConstContainer = Dictionary<string, List<RawConst>>;
    using RawDataContainer = Dictionary<string, List<RawSheetData>>;
    using RawEnumContainer = Dictionary<string, List<RawEnum>>;
    using SchemaContainer = Dictionary<string, SchemaSet>;

    public class CompleteContainers
    {
        public SchemaContainer Schema { get; set; } = new SchemaContainer();
        public DataContainer Data { get; set; } = new DataContainer();
        public EnumContainer Enum { get; set; } = new EnumContainer();
        public ConstContainer Const { get; set; } = new ConstContainer();
    }

    public class Context
    {
        public static string BUILD_VERSION = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        public const string CACHE_DIRECTORY = "cache";
        public const string RAW_CACHE_FILE = "raw";
        public const string ERROR_FILE = "err";
        public readonly static string RAW_CACHE_PATH = GetCacheFilePath(RAW_CACHE_FILE);
        public readonly static string ERROR_CACHE_PATH = GetCacheFilePath(ERROR_FILE);

        private readonly CastValueFactory _castFactory;
        private readonly ConcurrentDictionary<object, object> _dp = new ConcurrentDictionary<object, object>();

        [JsonIgnore]
        public static Config Config { get; private set; } = ReadConfigFile();

        [JsonIgnore]
        public string Output = "output";

        [JsonIgnore]
        public static JObject DSL { get; private set; } = ReadDslFile();

        public RawConstContainer RawConst { get; private set; } = new RawConstContainer();
        public RawEnumContainer RawEnum { get; private set; } = new RawEnumContainer();
        public RawDataContainer RawData { get; private set; } = new RawDataContainer();
        public Dictionary<string, string> CRC { get; private set; } = new Dictionary<string, string>();
        public string BuildVersion { get; set; } = BUILD_VERSION;

        [JsonIgnore] public CompleteContainers Result { get; set; } = new CompleteContainers();

        [JsonIgnore]
        public HashSet<string> RawAllTableNames => RawData.SelectMany(x => x.Value).Select(x => x.TableName).ToHashSet();

        [JsonIgnore]
        public HashSet<string> AllTableNames => Result.Schema.Keys.ToHashSet();

        [JsonIgnore]
        public HashSet<string> KeyTableNames => Result.Schema.Keys.Where(x => GetKey(x) != null).ToHashSet();

        static Context()
        {
            if (Directory.Exists(CACHE_DIRECTORY) == false)
                Directory.CreateDirectory(CACHE_DIRECTORY);
        }

        public Context()
        {
            _castFactory = new CastValueFactory(this);
        }

        public static Context operator +(Context ctx1, Context ctx2)
        {
            return new Context
            {
                RawEnum = ctx1.RawEnum.Concat(ctx2.RawEnum).ToDictionary(x => x.Key, x => x.Value),
                RawData = ctx1.RawData.Concat(ctx2.RawData).ToDictionary(x => x.Key, x => x.Value),
                RawConst = ctx1.RawConst.Concat(ctx2.RawConst).ToDictionary(x => x.Key, x => x.Value),
                CRC = ctx1.CRC.Concat(ctx2.CRC).ToDictionary(x => x.Key, x => x.Value),
            };
        }

        private static T ReadFileWithEnvironmentVariable<T>(string fname, Func<string, T> callback)
        {
            var file = Path.GetFileNameWithoutExtension(fname);
            var ext = Path.GetExtension(fname);
            var env = Environment.GetEnvironmentVariable("env");

            if (!string.IsNullOrEmpty(env))
            {
                var envFileName = $"{file}.{env}{ext}";
                if (File.Exists(envFileName))
                    fname = envFileName;
            }

            if (File.Exists(fname) == false)
                throw new FileNotFoundException();

            return callback.Invoke(File.ReadAllText(fname));
        }

        private static Config ReadConfigFile()
        {
            return ReadFileWithEnvironmentVariable("config.json", contents => 
            {
                return JsonConvert.DeserializeObject<Config>(contents);
            });
        }

        private static JObject ReadDslFile()
        {
            return ReadFileWithEnvironmentVariable("dsl.json", contents => 
            {
                return JObject.Parse(contents);
            });
        }

        private SchemaContainer GetSchema()
        {
            var result = new SchemaContainer();
            var group = RawData.SelectMany(x => x.Value).GroupBy(x => x.TableName).ToDictionary(x => x.Key, x => x.ToList());
            foreach (var (table, sheets) in group)
            {
                var based = sheets.FirstOrDefault()?.Based;
                var json = sheets.FirstOrDefault()?.Json;
                var columns = sheets.FirstOrDefault()?.Columns;
                var (boldColumns, normalColumns) = columns.Split();
                
                var root = string.Format(Config.ParentTableFormat, table);

                if (boldColumns != null)
                {
                    var schemaSet = new SchemaSet(null, root);
                    foreach (var column in boldColumns)
                    {
                        schemaSet.Add(column.Name, new Model.SchemaData
                        {
                            Name = column.Name,
                            Type = column.Type,
                            Scope = column.Scope
                        });
                    }
                    
                    result.Add(string.Format(Config.ParentTableFormat, table), schemaSet);
                }

                if (normalColumns != null)
                {
                    var schemaSet = new SchemaSet(based, json);
                    if (boldColumns != null)
                    {
                        var parentKeyColumn = boldColumns.FirstOrDefault(x => Util.Type.IsPrimaryKey(x.Type, out _));
                        schemaSet.Add(Config.ParentPropName, new Model.SchemaData
                        {
                            Name = Config.ParentPropName,
                            Type = $"(${root})",
                            Scope = parentKeyColumn.Scope
                        });
                    }

                    foreach (var column in normalColumns)
                    {
                        var inherited = string.IsNullOrEmpty(based) == false && (group[based].FirstOrDefault()?.Columns.Select(x => x.Name).Contains(column.Name) ?? false);

                        schemaSet.Add(column.Name, new Model.SchemaData
                        {
                            Name = column.Name,
                            Type = column.Type,
                            Scope = column.Scope,
                            Inherited = inherited
                        });
                    }
                    result.Add(table, schemaSet);
                }
            }

            return result;
        }

        public string GetRootTableType(string type, bool recursion = true)
        {
            if (Util.Type.IsRelation(type, out var rel))
            {
                var naked = Util.Type.Nake(rel);
                var nullable = Util.Type.IsNullable(rel);

                if (naked.Contains("."))
                {
                    var split = naked.Split(".");
                    naked = split[0];
                    var refer = split[1];

                    if (Result.Schema.TryGetValue(naked, out var schemaSet) == false)
                        throw new LogicException($"{naked} 테이블은 정의되지 않았습니다.");

                    if (schemaSet.TryGetValue(refer, out var x) == false)
                        throw new LogicException($"{refer}는 {naked} 테이블에 정의되지 않았습니다.");

                    type = Util.Type.Nake(x.Type, Util.NakeFlag.Key);
                }
                else
                {
                    if (Result.Schema.TryGetValue(naked, out var schemaSet) == false)
                        throw new LogicException($"{naked} 테이블은 정의되지 않았습니다.");

                    var key = schemaSet.Key;
                    if (key == null)
                        throw new LogicException($"{naked} 테이블은 키 정의가 되지 않았습니다.");

                    type = Util.Type.Nake(schemaSet[key].Type, Util.NakeFlag.Key);
                }
                if (recursion)
                    type = GetRootTableType(type, recursion);

                if (nullable)
                    type = Util.Type.MakeNullable(type);

                return type;
            }
            else if (Util.Type.IsSequence(type, out _))
            {
                var nullable = Util.Type.IsNullable(type);
                if (nullable)
                    return Util.Type.MakeNullable("int");
                else
                    return "int";
            }
            else
            {
                return Util.Type.Nake(type, NakeFlag.All & ~NakeFlag.Nullable);
            }
        }

        public object Cast(string type, object value)
        {
            return _castFactory.Build(type, value);
        }

        public SchemaSet GetScopeSchema(string table, Scope scope, ScopeFilterType scopeFilterType = ScopeFilterType.Match)
        {
            if (Result.Schema.TryGetValue(table, out var schema) == false)
                return null;

            var filter = schema.Where(pair => 
            {
                return scopeFilterType switch
                {
                    ScopeFilterType.Match => scope == pair.Value.Scope,
                    ScopeFilterType.Contains => pair.Value.Scope.HasFlag(scope),
                    _ => throw new InvalidOperationException(),
                };
            }).ToDictionary(x => x.Key, x => x.Value);
            if (filter.Count == 0)
                return null;

            var schemaSet = new SchemaSet(schema.Based, schema.Json);
            foreach (var (k, v) in filter)
            {
                schemaSet.Add(k, v);
            }

            return schemaSet;
        }

        public void Arrange()
        {
            Result.Enum = RawEnum.SelectMany(x => x.Value).GroupBy(x => x.Table).ToDictionary(x => x.Key, x => x.SelectMany(x => x.Values).ToDictionary(x => x.Key, x => x.Value));
            var dslFunctionTypes = new Dictionary<string, List<object>>();
            Result.Enum.Add(Config.DslTypeEnumName, dslFunctionTypes);
            int i = 0;
            foreach (var dsl in DSL)
            {
                dslFunctionTypes.Add(dsl.Key, new List<object> { i++ });
            }
            Result.Schema = GetSchema();
            Result.Data = new DataTypeCaster(this).Run().GroupBy(x => x.FileName).ToDictionary(x => x.Key, x => 
            {
                return x.GroupBy(x => x.TableName).ToDictionary(x => x.Key, x => x.OrderBy(x => x.SheetName).ToList());
            });
            Result.Const = RawConst.SelectMany(x => x.Value).GroupBy(x => x.TableName).ToDictionary(x => x.Key, x =>
            {
                return x.OrderBy(x => x.FileName).ToDictionary(x => x.Name, x => new ConstData
                {
                    Name = x.Name,
                    Type = x.Type,
                    Scope = x.Scope,
                    Value = Cast(x.Type, x.Value)
                });
            });
        }

        public Dictionary<string, object> GetEffectiveSortedDataSet(Scope scope) // {json:container}
        {
            // {table:rows}
            var tableRows = Result.Data.SelectMany(x => x.Value).GroupBy(x => x.Key).ToDictionary(x => x.Key, x =>
            {
                var table = x.Key;
                return x.SelectMany(x => x.Value).SelectMany(x => x.Rows).ToList();
            });

            // {json:rows}
            var jsonRows = new Dictionary<string/*json*/, List<Dictionary<string/*column*/, object/*value*/>>>();
            foreach (var g in Result.Schema.GroupBy(x => x.Value.Json))
            {
                var json = g.Key;
                var rows = new List<Dictionary<string, object>>();
                foreach (var tableName in g.Select(x => x.Key))
                {
                    var schema = Result.Schema[tableName].Values.Where(x => x.Scope.HasFlag(scope));
                    var columns = schema.Select(x => x.Name).ToHashSet();
                    if (columns.Count == 0)
                        continue;

                    var scopedRows = tableRows[tableName]
                        .Select(row => row.Where(x => columns.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value))
                        .Where(x => x.Count > 0)
                        .ToList();
                    rows.AddRange(scopedRows);
                }

                jsonRows.Add(json, rows);
            }

            return jsonRows.ToDictionary(x => x.Key, x =>
            {
                var json = x.Key;
                var rows = x.Value;
                return new DataContainerFactory(scope).Build(this, json, rows);
            }).Where(pair => pair.Value != null).ToDictionary(x => x.Key, x => x.Value);
        }

        public Dictionary<string, Dictionary<string, object>> GetEffectiveSortedDataSetWithSheetName(Scope scope) // {sheet:{table:container}}
        {
            return Result.Data.SelectMany(x => x.Value.SelectMany(x => x.Value)).GroupBy(x => x.SheetName).ToDictionary(x => x.Key, x =>
            {
                return x.GroupBy(x => x.TableName).ToDictionary(x => x.Key, x =>
                {
                    var tableName = x.Key;
                    var schema = Result.Schema[tableName].Values.Where(x => x.Scope.HasFlag(scope));
                    var columns = schema.Select(x => x.Name).ToHashSet();
                    if (columns.Count == 0)
                        return null;

                    var rows = x.SelectMany(x => x.Rows)
                        .Select(row => row.Where(x => columns.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value))
                        .Where(x => x.Count > 0)
                        .ToList();

                    return new DataContainerFactory(scope).Build(this, x.Key, rows);
                }).Where(pair => pair.Value != null).ToDictionary(x => x.Key, x => x.Value);
            });
        }

        public static string GetCacheFilePath(string fileName)
        {
            return Path.Combine(CACHE_DIRECTORY, $"{fileName}.dat");
        }

        public List<RawDataColumns> GetRawColumns(string tableName)
        {
            var key = $"GetRawColumns_{tableName}";
            return _dp.GetOrAdd(key, _ =>
            {
                return RawData.SelectMany(x => x.Value)
                .Where(x => x.TableName == tableName)
                .FirstOrDefault()?.Columns;
            }) as List<RawDataColumns>;
        }

        public RawSheetData FindRawSheetData(RawDataColumns column)
        {
            return RawData.SelectMany(x => x.Value).FirstOrDefault(x => x.Columns.Contains(column));
        }

        public static bool SplitReferenceType(string type, out string tableName, out string columnName)
        {
            var split = type.Split('.');
            if (split.Length > 2)
            {
                tableName = columnName = null;
                return false;
            }

            tableName = split[0];
            columnName = split.ElementAtOrDefault(1);
            return true;
        }

        public List<Dictionary<string, object>> GetValues(string tableName)
        {
            var key = $"GetValues_{tableName}";
            return _dp.GetOrAdd(key, _ => 
            {
                return Result.Data
                .SelectMany(x => x.Value)
                .Where(x => x.Key == tableName)
                .SelectMany(x => x.Value)
                .SelectMany(x => x.Rows)
                .ToList();
            }) as List<Dictionary<string, object>>;
        }

        public IEnumerable<string> GetTableNamesFromJson(string json)
        {
            foreach (var (tableName, schema) in Result.Schema)
            {
                if (schema.Json == json)
                    yield return tableName;
            }
        }

        public IReadOnlyList<object> GetValues(string tableName, string columnName)
        {
            var key = $"GetValues_{tableName}_{columnName}";
            return _dp.GetOrAdd(key, _ => GetValues(tableName).Select(x => x[columnName]).ToList()) as List<object>;
        }

        public IReadOnlyList<object> GetValuesFromJson(string jsonName, string columnName)
        {
            var key = $"GetValuesFromJson_{jsonName}_{columnName}";
            return _dp.GetOrAdd(key, _ => 
            {
                var tableNames = GetTableNamesFromJson(jsonName).ToList();
                return tableNames.SelectMany(tableName =>
                {
                    return GetValues(tableName).Select(x => x[columnName]).ToList();
                }).ToList();
            }) as List<object>;
        }

        public SchemaData GetKey(string tableName)
        {
            var key = $"GetKey_{tableName}";
            return _dp.GetOrAdd(key, __ =>
            {
                var values = Result.Schema[tableName].Values;
                return values.FirstOrDefault(x => Util.Type.IsGroupKey(x.Type, out _)) ??
                    values.FirstOrDefault(x => Util.Type.IsPrimaryKey(x.Type, out _));
            }) as SchemaData;
        }

        public bool ContainsColumn(string tableName, string columnName)
        {
            if (Result.Schema.TryGetValue(tableName, out var schema) == false)
                return false;

            return schema.ContainsKey(columnName);
        }

        public string GetCSharpSerializeCode(string type, string name)
        {
            var naked = Util.Type.Nake(GetRootTableType(type));
            if (naked != "int")
                return string.Empty;

            var nullable = Util.Type.IsNullable(type);
            var prefix = string.Empty;
            if (nullable)
                prefix = $"{name} == null ? (long?)null : (long?)";

            return $"{prefix}(long)";
        }

        public int GetInheritanceLevel(string tableName)
        {
            var based = Result.Schema[tableName].Based;
            if (string.IsNullOrEmpty(based))
                return 0;

            return 1 + GetInheritanceLevel(based);
        }
    }
}
