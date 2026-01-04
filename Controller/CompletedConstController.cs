using ExcelTableConverter.Model;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ExcelTableConverter.Controller
{
    using ConstContainer = Dictionary<string, Dictionary<string, ConstData>>;

    /// <summary>
    /// Manages completed constant operations and provides access to constant data
    /// Handles constant value operations, retrieval, and validation for completed data
    /// </summary>
    public class CompletedConstController : IReadOnlyDictionary<string, Dictionary<string, ConstData>>
    {
        private readonly ConcurrentDictionary<object, object> _cache = new();
        private readonly Context _context;

        /// <summary>
        /// Gets the completed constant container
        /// </summary>
        public ConstContainer Container { get; private set; }

        public IEnumerable<string> Keys => Container.Keys;

        public IEnumerable<Dictionary<string, ConstData>> Values => Container.Values;

        public int Count => Container.Count;

        public Dictionary<string, ConstData> this[string key] => Container[key];

        /// <summary>
        /// Initializes a new instance of the CompletedConstController class
        /// </summary>
        /// <param name="context">Context instance for configuration and data access</param>
        public CompletedConstController(Context context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            Container = new ConstContainer();
        }

        /// <summary>
        /// Initializes a new instance of the CompletedConstController class with existing container
        /// </summary>
        /// <param name="context">Context instance for configuration and data access</param>
        /// <param name="container">Existing constant container</param>
        public CompletedConstController(Context context, ConstContainer container)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            Container = container ?? new ConstContainer();
        }

        /// <summary>
        /// Updates the constant container with new data
        /// </summary>
        /// <param name="newContainer">New constant container to replace current one</param>
        public void UpdateContainer(ConstContainer newContainer)
        {
            Container = newContainer ?? new ConstContainer();
            ClearCache(); // Clear cache when container is updated
        }

        /// <summary>
        /// Builds and updates constant data from source const controller
        /// </summary>
        /// <param name="sourceConstController">Source const controller</param>
        /// <param name="castMethod">Method to cast values to appropriate types</param>
        public void BuildFromSourceData(SourceConstController sourceConstController, Func<string, object, IExcelFileTrackable, object> castMethod)
        {
            if (sourceConstController == null)
                throw new ArgumentNullException(nameof(sourceConstController));

            if (castMethod == null)
                throw new ArgumentNullException(nameof(castMethod));

            var constContainer = sourceConstController.GetAllConsts().GroupBy(x => x.TableName)
                .ToDictionary(x => x.Key, x =>
                {
                    return x.OrderBy(x => x.FileName)
                            .ThenBy(x => x.SheetName)
                            .ToDictionary(x => x.Name, x => new ConstData
                            {
                                Name = x.Name,
                                Type = x.Type,
                                Scope = x.Scope,
                                Value = castMethod(x.Type, x.Value, null)
                            });
                });

            UpdateContainer(constContainer);
        }

        /// <summary>
        /// Gets all table names from completed constant data
        /// </summary>
        /// <returns>Set of all table names</returns>
        public HashSet<string> GetAllTableNames()
        {
            var key = "GetAllTableNames";
            return _cache.GetOrAdd(key, _ => Container.Keys.ToHashSet()) as HashSet<string>;
        }

        /// <summary>
        /// Gets a constant value by table and constant name
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <param name="constName">Name of the constant</param>
        /// <returns>Constant data if found, null otherwise</returns>
        public ConstData GetConst(string tableName, string constName)
        {
            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(constName))
                return null;

            var cacheKey = $"GetConst_{tableName}_{constName}";
            return _cache.GetOrAdd(cacheKey, _ =>
            {
                if (!Container.TryGetValue(tableName, out var constants))
                    return null;

                return constants.TryGetValue(constName, out var constData) ? constData : null;
            }) as ConstData;
        }

        /// <summary>
        /// Gets all constants for a specific table
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <returns>Dictionary of constants for the table, or null if not found</returns>
        public Dictionary<string, ConstData> GetConstsByTable(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                return null;

            return Container.TryGetValue(tableName, out var constants) ? constants : null;
        }

        /// <summary>
        /// Gets constant value by table and constant name
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <param name="constName">Name of the constant</param>
        /// <returns>Constant value if found, null otherwise</returns>
        public object GetConstValue(string tableName, string constName)
        {
            var constData = GetConst(tableName, constName);
            return constData?.Value;
        }

        /// <summary>
        /// Gets all constant names for a specific table
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <returns>Set of constant names for the table</returns>
        public HashSet<string> GetConstNames(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                return new HashSet<string>();

            var cacheKey = $"GetConstNames_{tableName}";
            return _cache.GetOrAdd(cacheKey, _ =>
            {
                if (!Container.TryGetValue(tableName, out var constants))
                    return new HashSet<string>();

                return constants.Keys.ToHashSet();
            }) as HashSet<string>;
        }

        /// <summary>
        /// Checks if a constant exists
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <param name="constName">Name of the constant</param>
        /// <returns>True if constant exists</returns>
        public bool ContainsConst(string tableName, string constName)
        {
            return GetConst(tableName, constName) != null;
        }

        /// <summary>
        /// Checks if a table exists in completed constant data
        /// </summary>
        /// <param name="tableName">Name of the table to check</param>
        /// <returns>True if table exists</returns>
        public bool ContainsTable(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                return false;

            return Container.ContainsKey(tableName);
        }

        /// <summary>
        /// Gets the number of constant tables
        /// </summary>
        /// <returns>Number of constant tables</returns>
        public int GetTableCount()
        {
            return Container.Count;
        }

        /// <summary>
        /// Gets constants filtered by scope
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <param name="scope">Scope to filter by</param>
        /// <returns>Dictionary of constants matching the scope</returns>
        public Dictionary<string, ConstData> GetConstsByScope(string tableName, Scope scope)
        {
            if (string.IsNullOrEmpty(tableName))
                return new Dictionary<string, ConstData>();

            var cacheKey = $"GetConstsByScope_{tableName}_{scope}";
            return _cache.GetOrAdd(cacheKey, _ =>
            {
                if (!Container.TryGetValue(tableName, out var constants))
                    return new Dictionary<string, ConstData>();

                return constants.Where(pair => pair.Value.Scope.HasFlag(scope))
                    .ToDictionary(x => x.Key, x => x.Value);
            }) as Dictionary<string, ConstData>;
        }

        /// <summary>
        /// Gets constants by type
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <param name="type">Type to filter by</param>
        /// <returns>Dictionary of constants matching the type</returns>
        public Dictionary<string, ConstData> GetConstsByType(string tableName, string type)
        {
            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(type))
                return new Dictionary<string, ConstData>();

            var cacheKey = $"GetConstsByType_{tableName}_{type}";
            return _cache.GetOrAdd(cacheKey, _ =>
            {
                if (!Container.TryGetValue(tableName, out var constants))
                    return new Dictionary<string, ConstData>();

                return constants.Where(pair => pair.Value.Type == type)
                    .ToDictionary(x => x.Key, x => x.Value);
            }) as Dictionary<string, ConstData>;
        }

        /// <summary>
        /// Validates all completed constant data for consistency
        /// </summary>
        /// <returns>True if all data is valid</returns>
        public bool ValidateAll()
        {
            try
            {
                // Basic validation - check for null or empty data
                foreach (var (tableName, constants) in Container)
                {
                    if (string.IsNullOrEmpty(tableName))
                        return false;

                    if (constants == null)
                        return false;

                    foreach (var (constName, constData) in constants)
                    {
                        if (string.IsNullOrEmpty(constName))
                            return false;

                        if (constData == null)
                            return false;

                        if (string.IsNullOrEmpty(constData.Name))
                            return false;

                        if (string.IsNullOrEmpty(constData.Type))
                            return false;

                        // Value can be null for some constants, so we don't validate it
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
        /// Gets statistics about completed constant data
        /// </summary>
        /// <returns>Completed constant statistics</returns>
        public CompletedConstStatistics GetStatistics()
        {
            return new CompletedConstStatistics
            {
                TableCount = GetTableCount(),
                TotalConstCount = Container.Values.Sum(x => x.Count)
            };
        }

        /// <summary>
        /// Gets all constants flattened into a single dictionary
        /// </summary>
        /// <returns>Dictionary mapping table.constant names to their data</returns>
        public Dictionary<string, ConstData> GetFlattenedConstData()
        {
            var cacheKey = "GetFlattenedConstData";
            return _cache.GetOrAdd(cacheKey, _ =>
            {
                var result = new Dictionary<string, ConstData>();
                foreach (var (tableName, constants) in Container)
                {
                    foreach (var (constName, constData) in constants)
                    {
                        var key = $"{tableName}.{constName}";
                        result[key] = constData;
                    }
                }
                return result;
            }) as Dictionary<string, ConstData>;
        }

        /// <summary>
        /// Gets all constant values flattened into a single dictionary
        /// </summary>
        /// <returns>Dictionary mapping table.constant names to their values</returns>
        public Dictionary<string, object> GetFlattenedConstValues()
        {
            var cacheKey = "GetFlattenedConstValues";
            return _cache.GetOrAdd(cacheKey, _ =>
            {
                var result = new Dictionary<string, object>();
                foreach (var (tableName, constants) in Container)
                {
                    foreach (var (constName, constData) in constants)
                    {
                        var key = $"{tableName}.{constName}";
                        result[key] = constData.Value;
                    }
                }
                return result;
            }) as Dictionary<string, object>;
        }

        public bool ContainsKey(string key)
        {
            return Container.ContainsKey(key);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out Dictionary<string, ConstData> value)
        {
            return Container.TryGetValue(key, out value);
        }

        public IEnumerator<KeyValuePair<string, Dictionary<string, ConstData>>> GetEnumerator()
        {
            return Container.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Container.GetEnumerator();
        }

        public byte[] ToBytes()
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Container));
        }

        public bool FromBytes(byte[] bytes)
        {
            try
            {
                Container = JsonConvert.DeserializeObject<ConstContainer>(Encoding.UTF8.GetString(bytes));
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
    /// Statistics information about completed constant data
    /// </summary>
    public class CompletedConstStatistics
    {
        /// <summary>
        /// Gets or sets the number of constant tables
        /// </summary>
        public int TableCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of constants across all tables
        /// </summary>
        public int TotalConstCount { get; set; }
    }
}