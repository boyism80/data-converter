using ExcelTableConverter.Model;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ExcelTableConverter.Controller
{
    using SourceConstContainer = Dictionary<string, List<SourceConst>>;

    /// <summary>
    /// Manages source constant operations and provides access to source constant data
    /// Handles constant value operations and validation for source data
    /// </summary>
    public class SourceConstController : IReadOnlyDictionary<string, List<SourceConst>>
    {
        private readonly ConcurrentDictionary<object, object> _cache = new();
        private readonly Context _context;

        /// <summary>
        /// Gets the source constant container
        /// </summary>
        public SourceConstContainer Container { get; private set; }

        public IEnumerable<string> Keys => Container.Keys;

        public IEnumerable<List<SourceConst>> Values => Container.Values;

        public int Count => Container.Count;

        public List<SourceConst> this[string key] => Container[key];

        /// <summary>
        /// Initializes a new instance of the SourceConstController class
        /// </summary>
        /// <param name="context">Context instance for configuration and data access</param>
        public SourceConstController(Context context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            Container = new SourceConstContainer();
        }

        /// <summary>
        /// Initializes a new instance of the SourceConstController class with existing container
        /// </summary>
        /// <param name="context">Context instance for configuration and data access</param>
        /// <param name="container">Existing source constant container</param>
        public SourceConstController(Context context, SourceConstContainer container)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            Container = container ?? new SourceConstContainer();
        }

        /// <summary>
        /// Gets a constant value by table and constant name
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <param name="constName">Name of the constant</param>
        /// <returns>Source constant if found, null otherwise</returns>
        public SourceConst GetConst(string tableName, string constName)
        {
            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(constName))
                return null;

            var key = $"GetConst_{tableName}_{constName}";
            return _cache.GetOrAdd(key, _ =>
            {
                return Container.SelectMany(x => x.Value)
                    .FirstOrDefault(x => x.TableName == tableName && x.Name == constName);
            }) as SourceConst;
        }

        /// <summary>
        /// Gets all constants for a specific table
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <returns>List of constants for the table</returns>
        public List<SourceConst> GetConstsByTable(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                return new List<SourceConst>();

            var key = $"GetConstsByTable_{tableName}";
            return _cache.GetOrAdd(key, _ =>
            {
                return Container.SelectMany(x => x.Value)
                    .Where(x => x.TableName == tableName)
                    .ToList();
            }) as List<SourceConst>;
        }

        /// <summary>
        /// Gets constants for a specific file
        /// </summary>
        /// <param name="fileName">Name of the file</param>
        /// <returns>List of constants for the file</returns>
        public List<SourceConst> GetConstsByFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return new List<SourceConst>();

            return Container.TryGetValue(fileName, out var consts) ? consts : new List<SourceConst>();
        }

        /// <summary>
        /// Gets all table names from source constant data
        /// </summary>
        /// <returns>Set of all table names</returns>
        public HashSet<string> GetAllTableNames()
        {
            var key = "GetAllTableNames";
            return _cache.GetOrAdd(key, _ =>
            {
                return Container.SelectMany(x => x.Value)
                    .Select(x => x.TableName)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToHashSet();
            }) as HashSet<string>;
        }

        /// <summary>
        /// Gets all constants from all files
        /// </summary>
        /// <returns>Enumerable of all source constants</returns>
        public IEnumerable<SourceConst> GetAllConsts()
        {
            return Container.SelectMany(x => x.Value);
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

            var key = $"GetConstNames_{tableName}";
            return _cache.GetOrAdd(key, _ =>
            {
                return Container.SelectMany(x => x.Value)
                    .Where(x => x.TableName == tableName)
                    .Select(x => x.Name)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToHashSet();
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
        /// Checks if a table exists in source constant data
        /// </summary>
        /// <param name="tableName">Name of the table to check</param>
        /// <returns>True if table exists</returns>
        public bool ContainsTable(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                return false;

            return GetAllTableNames().Contains(tableName);
        }

        /// <summary>
        /// Gets the number of constant tables in source data
        /// </summary>
        /// <returns>Number of constant tables</returns>
        public int GetTableCount()
        {
            return GetAllTableNames().Count;
        }

        /// <summary>
        /// Gets all file names that contain source constant data
        /// </summary>
        /// <returns>Set of file names</returns>
        public HashSet<string> GetAllFileNames()
        {
            var key = "GetAllFileNames";
            return _cache.GetOrAdd(key, _ =>
            {
                return Container.Keys.ToHashSet();
            }) as HashSet<string>;
        }

        /// <summary>
        /// Validates all source constant data for consistency
        /// </summary>
        /// <returns>True if all data is valid</returns>
        public bool ValidateAll()
        {
            try
            {
                // Basic validation - check for null or empty data
                foreach (var (fileName, constList) in Container)
                {
                    if (string.IsNullOrEmpty(fileName))
                        return false;

                    if (constList == null)
                        return false;

                    foreach (var sourceConst in constList)
                    {
                        if (sourceConst == null)
                            return false;

                        if (string.IsNullOrEmpty(sourceConst.TableName))
                            return false;

                        if (string.IsNullOrEmpty(sourceConst.Name))
                            return false;

                        if (string.IsNullOrEmpty(sourceConst.Type))
                            return false;

                        // Value can be null or empty for some constants, so we don't validate it
                    }
                }

                // Check for duplicate constants within the same table
                var duplicates = Container.SelectMany(x => x.Value)
                    .GroupBy(x => new { x.TableName, x.Name })
                    .Where(g => g.Count() > 1)
                    .ToList();

                if (duplicates.Any())
                    return false;

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
        /// Gets statistics about source constant data
        /// </summary>
        /// <returns>Source constant statistics</returns>
        public SourceConstStatistics GetStatistics()
        {
            return new SourceConstStatistics
            {
                FileCount = GetAllFileNames().Count,
                TableCount = GetTableCount(),
                TotalConstCount = Container.SelectMany(x => x.Value).Count()
            };
        }

        /// <summary>
        /// Gets all constants grouped by table name
        /// </summary>
        /// <returns>Dictionary mapping table names to their constants</returns>
        public Dictionary<string, List<SourceConst>> GetConstsByTableGrouped()
        {
            var key = "GetConstsByTableGrouped";
            return _cache.GetOrAdd(key, _ =>
            {
                return Container.SelectMany(x => x.Value)
                    .GroupBy(x => x.TableName)
                    .ToDictionary(g => g.Key, g => g.ToList());
            }) as Dictionary<string, List<SourceConst>>;
        }

        /// <summary>
        /// Validates that constant types are valid
        /// </summary>
        /// <returns>True if all constant types are valid</returns>
        public bool ValidateConstantTypes()
        {
            try
            {
                var validTypes = new HashSet<string> { "int", "string", "float", "double", "bool", "long" };

                var invalidConstants = Container.SelectMany(x => x.Value)
                    .Where(x => !validTypes.Contains(x.Type?.ToLower()))
                    .ToList();

                return !invalidConstants.Any();
            }
            catch
            {
                return false;
            }
        }

        public bool ContainsKey(string key)
        {
            return Container.ContainsKey(key);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out List<SourceConst> value)
        {
            return Container.TryGetValue(key, out value);
        }

        public IEnumerator<KeyValuePair<string, List<SourceConst>>> GetEnumerator()
        {
            return Container.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Container.GetEnumerator();
        }

        public void Add(string key, List<SourceConst> value)
        {
            Container.Add(key, value);
        }

        public void Remove(string key)
        {
            Container.Remove(key);
        }

        public byte[] ToBytes()
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Container));
        }

        public bool FromBytes(byte[] bytes)
        {
            try
            {
                Container = JsonConvert.DeserializeObject<SourceConstContainer>(Encoding.UTF8.GetString(bytes));
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
    /// Statistics information about source constant data
    /// </summary>
    public class SourceConstStatistics
    {
        /// <summary>
        /// Gets or sets the number of files
        /// </summary>
        public int FileCount { get; set; }

        /// <summary>
        /// Gets or sets the number of constant tables
        /// </summary>
        public int TableCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of constants
        /// </summary>
        public int TotalConstCount { get; set; }
    }
}