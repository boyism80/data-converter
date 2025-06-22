using ExcelTableConverter.Model;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ExcelTableConverter.Controller
{
    using SourceEnumContainer = Dictionary<string, List<SourceEnum>>;

    /// <summary>
    /// Manages source enum operations and provides access to source enum data
    /// Handles enum type operations and validation for source data
    /// </summary>
    public class SourceEnumController : IReadOnlyDictionary<string, List<SourceEnum>>
    {
        private readonly ConcurrentDictionary<object, object> _cache = new ConcurrentDictionary<object, object>();
        private readonly Context _context;

        /// <summary>
        /// Gets the source enum container
        /// </summary>
        public SourceEnumContainer Container { get; private set; }

        public IEnumerable<string> Keys => Container.Keys;

        public IEnumerable<List<SourceEnum>> Values => Container.Values;

        public int Count => Container.Count;

        public List<SourceEnum> this[string key] => Container[key];

        /// <summary>
        /// Initializes a new instance of the SourceEnumController class
        /// </summary>
        /// <param name="context">Context instance for configuration and data access</param>
        public SourceEnumController(Context context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            Container = new SourceEnumContainer();
        }

        /// <summary>
        /// Initializes a new instance of the SourceEnumController class with existing container
        /// </summary>
        /// <param name="context">Context instance for configuration and data access</param>
        /// <param name="container">Existing source enum container</param>
        public SourceEnumController(Context context, SourceEnumContainer container)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            Container = container ?? new SourceEnumContainer();
        }

        /// <summary>
        /// Tries to get a source enum by type name
        /// </summary>
        /// <param name="type">Type name to search for</param>
        /// <param name="sourceEnum">Output source enum if found</param>
        /// <returns>True if enum found</returns>
        public bool TryGetEnum(string type, out SourceEnum sourceEnum)
        {
            sourceEnum = null;

            if (string.IsNullOrEmpty(type))
                return false;

            foreach (var enumList in Container.Values)
            {
                var found = enumList.FirstOrDefault(x => x.SheetName == type);
                if (found != null)
                {
                    sourceEnum = found;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets all enum types from source data
        /// </summary>
        /// <returns>Set of all enum type names</returns>
        public HashSet<string> GetAllEnumTypes()
        {
            var key = "GetAllEnumTypes";
            return _cache.GetOrAdd(key, _ =>
            {
                return Container.SelectMany(x => x.Value)
                    .Select(x => x.SheetName)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToHashSet();
            }) as HashSet<string>;
        }

        /// <summary>
        /// Gets all enums from all files
        /// </summary>
        /// <returns>Enumerable of all source enums</returns>
        public IEnumerable<SourceEnum> GetAllEnums()
        {
            return Container.SelectMany(x => x.Value);
        }

        /// <summary>
        /// Gets all enum tables from source data
        /// </summary>
        /// <returns>Set of all enum table names</returns>
        public HashSet<string> GetAllTableNames()
        {
            var key = "GetAllTableNames";
            return _cache.GetOrAdd(key, _ =>
            {
                return Container.SelectMany(x => x.Value)
                    .Select(x => x.Table)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToHashSet();
            }) as HashSet<string>;
        }

        /// <summary>
        /// Gets source enums for a specific table
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <returns>List of source enums for the table</returns>
        public List<SourceEnum> GetEnumsByTable(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                return new List<SourceEnum>();

            var key = $"GetEnumsByTable_{tableName}";
            return _cache.GetOrAdd(key, _ =>
            {
                return Container.SelectMany(x => x.Value)
                    .Where(x => x.Table == tableName)
                    .ToList();
            }) as List<SourceEnum>;
        }

        /// <summary>
        /// Gets source enums for a specific file
        /// </summary>
        /// <param name="fileName">Name of the file</param>
        /// <returns>List of source enums for the file</returns>
        public List<SourceEnum> GetEnumsByFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return new List<SourceEnum>();

            return Container.TryGetValue(fileName, out var enums) ? enums : new List<SourceEnum>();
        }

        /// <summary>
        /// Checks if an enum type exists in source data
        /// </summary>
        /// <param name="type">Type name to check</param>
        /// <returns>True if enum type exists</returns>
        public bool ContainsEnumType(string type)
        {
            if (string.IsNullOrEmpty(type))
                return false;

            return GetAllEnumTypes().Contains(type);
        }

        /// <summary>
        /// Checks if an enum table exists in source data
        /// </summary>
        /// <param name="tableName">Table name to check</param>
        /// <returns>True if enum table exists</returns>
        public bool ContainsTable(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                return false;

            return GetAllTableNames().Contains(tableName);
        }

        /// <summary>
        /// Gets the number of enum tables in source data
        /// </summary>
        /// <returns>Number of enum tables</returns>
        public int GetTableCount()
        {
            return GetAllTableNames().Count;
        }

        /// <summary>
        /// Gets all file names that contain source enum data
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
        /// Validates all source enum data for consistency
        /// </summary>
        /// <returns>True if all data is valid</returns>
        public bool ValidateAll()
        {
            try
            {
                // Basic validation - check for null or empty data
                foreach (var (fileName, enumList) in Container)
                {
                    if (string.IsNullOrEmpty(fileName))
                        return false;

                    if (enumList == null)
                        return false;

                    foreach (var sourceEnum in enumList)
                    {
                        if (sourceEnum == null)
                            return false;

                        if (string.IsNullOrEmpty(sourceEnum.Table))
                            return false;

                        if (string.IsNullOrEmpty(sourceEnum.SheetName))
                            return false;

                        if (sourceEnum.Values == null)
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
        /// Tries to get a source enum by type name
        /// </summary>
        /// <param name="type">The enum type name to search for</param>
        /// <param name="sourceEnum">The found source enum, if any</param>
        /// <returns>True if the enum was found, false otherwise</returns>
        public bool TryGetSourceEnum(string type, out SourceEnum sourceEnum)
        {
            foreach (var x in Container.SelectMany(x => x.Value))
            {
                if (x.SheetName == type)
                {
                    sourceEnum = x;
                    return true;
                }
            }

            sourceEnum = null;
            return false;
        }

        /// <summary>
        /// Gets statistics about source enum data
        /// </summary>
        /// <returns>Source enum statistics</returns>
        public SourceEnumStatistics GetStatistics()
        {
            return new SourceEnumStatistics
            {
                FileCount = GetAllFileNames().Count,
                TableCount = GetTableCount(),
                EnumTypeCount = GetAllEnumTypes().Count,
                TotalEnumCount = Container.SelectMany(x => x.Value).Count()
            };
        }

        /// <summary>
        /// Gets enum values for a specific enum type
        /// </summary>
        /// <param name="enumType">Enum type name</param>
        /// <returns>Dictionary of enum values, or null if not found</returns>
        public Dictionary<string, List<object>> GetEnumValues(string enumType)
        {
            if (TryGetEnum(enumType, out var sourceEnum))
            {
                return sourceEnum.Values;
            }

            return null;
        }

        public bool ContainsKey(string key)
        {
            return Container.ContainsKey(key);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out List<SourceEnum> value)
        {
            return Container.TryGetValue(key, out value);
        }

        public IEnumerator<KeyValuePair<string, List<SourceEnum>>> GetEnumerator()
        {
            return Container.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Container.GetEnumerator();
        }

        public void Add(string key, List<SourceEnum> value)
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
                Container = JsonConvert.DeserializeObject<SourceEnumContainer>(Encoding.UTF8.GetString(bytes));
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
    /// Statistics information about source enum data
    /// </summary>
    public class SourceEnumStatistics
    {
        /// <summary>
        /// Gets or sets the number of files
        /// </summary>
        public int FileCount { get; set; }

        /// <summary>
        /// Gets or sets the number of enum tables
        /// </summary>
        public int TableCount { get; set; }

        /// <summary>
        /// Gets or sets the number of enum types
        /// </summary>
        public int EnumTypeCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of enums
        /// </summary>
        public int TotalEnumCount { get; set; }
    }
}