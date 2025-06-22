using ExcelTableConverter.Model;
using ExcelTableConverter.Services;
using ExcelTableConverter.Util;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ExcelTableConverter.Controller
{
    using SchemaContainer = Dictionary<string, SchemaSet>;

    /// <summary>
    /// Manages completed schema operations and provides access to schema data
    /// Handles table schema operations, key management, and inheritance relationships
    /// </summary>
    public class CompletedSchemaController : IReadOnlyDictionary<string, SchemaSet>
    {
        private readonly ConcurrentDictionary<object, object> _cache = new();
        private readonly Context _context;

        /// <summary>
        /// Gets the completed schema container
        /// </summary>
        public SchemaContainer Container { get; private set; }

        public IEnumerable<string> Keys => Container.Keys;

        public IEnumerable<SchemaSet> Values => Container.Values;

        public int Count => Container.Count;

        public SchemaSet this[string key] => Container[key];

        /// <summary>
        /// Initializes a new instance of the CompletedSchemaController class
        /// </summary>
        /// <param name="context">Context instance for configuration and data access</param>
        public CompletedSchemaController(Context context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            Container = new SchemaContainer();
        }

        /// <summary>
        /// Initializes a new instance of the CompletedSchemaController class with existing container
        /// </summary>
        /// <param name="context">Context instance for configuration and data access</param>
        /// <param name="container">Existing schema container</param>
        public CompletedSchemaController(Context context, SchemaContainer container)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            Container = container ?? new SchemaContainer();
        }

        /// <summary>
        /// Updates the schema container with new data
        /// </summary>
        /// <param name="newContainer">New schema container to replace current one</param>
        public void UpdateContainer(SchemaContainer newContainer)
        {
            Container = newContainer ?? new SchemaContainer();
            ClearCache(); // Clear cache when container is updated
        }

        /// <summary>
        /// Builds and updates schema from source data
        /// </summary>
        /// <param name="sourceDataController">Source data controller</param>
        /// <param name="configuration">Configuration service</param>
        public void BuildFromSourceData(SourceDataController sourceDataController, IConfigurationService configuration)
        {
            if (sourceDataController == null || configuration == null)
                throw new ArgumentNullException();

            var result = new SchemaContainer();
            var group = sourceDataController.GetAllSheetData().GroupBy(x => x.TableName).ToDictionary(x => x.Key, x => x.ToList());

            foreach (var (table, sheets) in group)
            {
                var based = sheets.FirstOrDefault()?.Based;
                var json = sheets.FirstOrDefault()?.Json;
                var columns = sheets.FirstOrDefault()?.Columns;
                var (boldColumns, normalColumns) = columns.Split();

                var root = string.Format(configuration.ParentTableFormat, table);

                if (boldColumns != null)
                {
                    var schemaSet = new SchemaSet(null, root);
                    foreach (var column in boldColumns)
                    {
                        schemaSet.Add(column.Name, new SchemaData
                        {
                            Name = column.Name,
                            Type = column.Type,
                            Scope = column.Scope
                        });
                    }

                    result.Add(string.Format(configuration.ParentTableFormat, table), schemaSet);
                }

                if (normalColumns != null)
                {
                    var schemaSet = new SchemaSet(based, json);
                    if (boldColumns != null)
                    {
                        var parentKeyColumn = boldColumns.FirstOrDefault(x => Util.Type.IsPrimaryKey(x.Type, out var _));
                        schemaSet.Add(configuration.ParentPropName, new SchemaData
                        {
                            Name = configuration.ParentPropName,
                            Type = $"(${root})",
                            Scope = parentKeyColumn.Scope
                        });
                    }

                    foreach (var column in normalColumns)
                    {
                        var inherited = string.IsNullOrEmpty(based) == false && (group[based].FirstOrDefault()?.Columns.Select(x => x.Name).Contains(column.Name) ?? false);

                        schemaSet.Add(column.Name, new SchemaData
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

            UpdateContainer(result);
        }

        /// <summary>
        /// Gets all table names from schema
        /// </summary>
        /// <returns>Set of all table names</returns>
        public HashSet<string> GetAllTableNames()
        {
            var key = "GetAllTableNames";
            return _cache.GetOrAdd(key, _ => Container.Keys.ToHashSet()) as HashSet<string>;
        }

        /// <summary>
        /// Gets table names that have keys defined
        /// </summary>
        /// <returns>Set of table names with keys</returns>
        public HashSet<string> GetKeyTableNames()
        {
            var key = "GetKeyTableNames";
            return _cache.GetOrAdd(key, _ =>
            {
                return Container.Keys.Where(tableName => GetKey(tableName) != null).ToHashSet();
            }) as HashSet<string>;
        }

        /// <summary>
        /// Gets the key schema data for a table
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <returns>Key schema data if found, null otherwise</returns>
        public SchemaData GetKey(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                return null;

            var cacheKey = $"GetKey_{tableName}";
            return _cache.GetOrAdd(cacheKey, _ =>
            {
                if (!Container.TryGetValue(tableName, out var schemaSet))
                    return null;

                var values = schemaSet.Values;
                return values.FirstOrDefault(x => Util.Type.IsGroupKey(x.Type, out var _)) ??
                       values.FirstOrDefault(x => Util.Type.IsPrimaryKey(x.Type, out var _));
            }) as SchemaData;
        }

        /// <summary>
        /// Gets schema for a specific table
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <returns>Schema set for the table, or null if not found</returns>
        public SchemaSet GetSchema(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                return null;

            return Container.TryGetValue(tableName, out var schema) ? schema : null;
        }

        /// <summary>
        /// Gets schema filtered by scope
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <param name="scope">Scope to filter by</param>
        /// <param name="scopeFilterType">Type of scope filtering</param>
        /// <returns>Filtered schema set, or null if not found</returns>
        public SchemaSet GetScopeSchema(string tableName, Scope scope, ScopeFilterType scopeFilterType = ScopeFilterType.Match)
        {
            if (!Container.TryGetValue(tableName, out var schema))
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

        /// <summary>
        /// Checks if a column exists in a table
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <param name="columnName">Name of the column</param>
        /// <returns>True if column exists</returns>
        public bool ContainsColumn(string tableName, string columnName)
        {
            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(columnName))
                return false;

            if (!Container.TryGetValue(tableName, out var schema))
                return false;

            return schema.ContainsKey(columnName);
        }

        /// <summary>
        /// Gets the inheritance level of a table
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <returns>Inheritance level (0 for no inheritance)</returns>
        public int GetInheritanceLevel(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                return 0;

            var cacheKey = $"GetInheritanceLevel_{tableName}";
            return (int)_cache.GetOrAdd(cacheKey, _ =>
            {
                if (!Container.TryGetValue(tableName, out var schema))
                    return 0;

                var based = schema.Based;
                if (string.IsNullOrEmpty(based))
                    return 0;

                return 1 + GetInheritanceLevel(based);
            });
        }

        /// <summary>
        /// Gets table names from a specific JSON name
        /// </summary>
        /// <param name="json">JSON name</param>
        /// <returns>Enumerable of table names</returns>
        public IEnumerable<string> GetTableNamesFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                yield break;

            foreach (var (tableName, schema) in Container)
            {
                if (schema.Json == json)
                    yield return tableName;
            }
        }

        /// <summary>
        /// Gets the number of tables in schema
        /// </summary>
        /// <returns>Number of tables</returns>
        public int GetTableCount()
        {
            return Container.Count;
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

                    if (TryGetValue(naked, out var schemaSet) == false)
                        throw new LogicException($"{naked} 테이블은 정의되지 않았습니다.".AsSpan());

                    if (schemaSet.TryGetValue(refer, out var x) == false)
                        throw new LogicException($"{refer}는 {naked} 테이블에 정의되지 않았습니다.".AsSpan());

                    type = Util.Type.Nake(x.Type, Util.NakeFlag.Key);
                }
                else
                {
                    if (TryGetValue(naked, out var schemaSet) == false)
                        throw new LogicException($"{naked} 테이블은 정의되지 않았습니다.".AsSpan());

                    var key = schemaSet.Key;
                    if (key == null)
                        throw new LogicException($"{naked} 테이블은 키 정의가 되지 않았습니다.".AsSpan());

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

        /// <summary>
        /// Validates all schema data for consistency
        /// </summary>
        /// <returns>True if all data is valid</returns>
        public bool ValidateAll()
        {
            try
            {
                // Basic validation - check for null or empty data
                foreach (var (tableName, schemaSet) in Container)
                {
                    if (string.IsNullOrEmpty(tableName))
                        return false;

                    if (schemaSet == null)
                        return false;

                    if (schemaSet.Count == 0)
                        return false;

                    foreach (var (columnName, schemaData) in schemaSet)
                    {
                        if (string.IsNullOrEmpty(columnName))
                            return false;

                        if (schemaData == null)
                            return false;

                        if (string.IsNullOrEmpty(schemaData.Name))
                            return false;

                        if (string.IsNullOrEmpty(schemaData.Type))
                            return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Clears all cached values
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
        }

        /// <summary>
        /// Gets statistics about schema data
        /// </summary>
        /// <returns>Schema statistics</returns>
        public CompletedSchemaStatistics GetStatistics()
        {
            return new CompletedSchemaStatistics
            {
                TableCount = GetTableCount(),
                KeyTableCount = GetKeyTableNames().Count,
                TotalColumnCount = Container.Values.Sum(schema => schema.Count)
            };
        }

        public bool ContainsKey(string key)
        {
            return Container.ContainsKey(key);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out SchemaSet value)
        {
            return Container.TryGetValue(key, out value);
        }

        public IEnumerator<KeyValuePair<string, SchemaSet>> GetEnumerator()
        {
            return Container.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Container.GetEnumerator();
        }

        public byte[] ToBytes()
        {
            try
            {
                return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Container));
            }
            catch (Exception)
            {
                return null;
            }
        }

        public bool FromBytes(byte[] bytes)
        {
            try
            {
                Container = JsonConvert.DeserializeObject<SchemaContainer>(Encoding.UTF8.GetString(bytes));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Clear()
        {
            Container.Clear();
        }
    }

    /// <summary>
    /// Statistics information about completed schema data
    /// </summary>
    public class CompletedSchemaStatistics
    {
        /// <summary>
        /// Gets or sets the number of tables
        /// </summary>
        public int TableCount { get; set; }

        /// <summary>
        /// Gets or sets the number of tables with keys
        /// </summary>
        public int KeyTableCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of columns across all tables
        /// </summary>
        public int TotalColumnCount { get; set; }
    }
}