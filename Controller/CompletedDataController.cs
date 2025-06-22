using ExcelTableConverter.Model;
using ExcelTableConverter.Worker;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ExcelTableConverter.Controller
{
    using DataContainer = Dictionary<string, Dictionary<string, List<DataConvertResult>>>;

    /// <summary>
    /// Manages completed data operations and provides access to converted data
    /// Handles data retrieval, value access, and data operations for completed data
    /// </summary>
    public class CompletedDataController : IReadOnlyDictionary<string, Dictionary<string, List<DataConvertResult>>>
    {
        private readonly ConcurrentDictionary<object, object> _cache = new();
        private readonly Context _context;

        /// <summary>
        /// Gets the completed data container
        /// </summary>
        public DataContainer Container { get; private set; }

        public IEnumerable<string> Keys => Container.Keys;

        public IEnumerable<Dictionary<string, List<DataConvertResult>>> Values => Container.Values;

        public int Count => Container.Count;

        public Dictionary<string, List<DataConvertResult>> this[string key] => Container[key];

        /// <summary>
        /// Initializes a new instance of the CompletedDataController class
        /// </summary>
        /// <param name="context">Context instance for configuration and data access</param>
        public CompletedDataController(Context context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            Container = new DataContainer();
        }

        /// <summary>
        /// Initializes a new instance of the CompletedDataController class with existing container
        /// </summary>
        /// <param name="context">Context instance for configuration and data access</param>
        /// <param name="container">Existing data container</param>
        public CompletedDataController(Context context, DataContainer container)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            Container = container ?? new DataContainer();
        }

        /// <summary>
        /// Updates the data container with new data
        /// </summary>
        /// <param name="newContainer">New data container to replace current one</param>
        public void UpdateContainer(DataContainer newContainer)
        {
            Container = newContainer ?? new DataContainer();
            ClearCache(); // Clear cache when container is updated
        }

        /// <summary>
        /// Builds and updates data from DataTypeCaster results
        /// </summary>
        /// <param name="dataConvertResults">Results from DataTypeCaster</param>
        public void BuildFromDataTypeCaster(IEnumerable<DataConvertResult> dataConvertResults)
        {
            if (dataConvertResults == null)
                throw new ArgumentNullException(nameof(dataConvertResults));

            var dataContainer = dataConvertResults.GroupBy(x => x.FileName)
                .ToDictionary(x => x.Key, x =>
                {
                    return x.GroupBy(x => x.TableName).ToDictionary(x => x.Key, x => x.OrderBy(x => x.SheetName).ToList());
                });

            UpdateContainer(dataContainer);
        }

        /// <summary>
        /// Gets all table names from completed data
        /// </summary>
        /// <returns>Set of all table names</returns>
        public HashSet<string> GetAllTableNames()
        {
            var key = "GetAllTableNames";
            return _cache.GetOrAdd(key, _ =>
            {
                return Container.SelectMany(x => x.Value.Keys).ToHashSet();
            }) as HashSet<string>;
        }

        /// <summary>
        /// Gets values for a specific table
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <returns>List of value dictionaries for the table</returns>
        public List<Dictionary<string, object>> GetValues(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                return new List<Dictionary<string, object>>();

            var cacheKey = $"GetValues_{tableName}";
            return _cache.GetOrAdd(cacheKey, _ =>
            {
                return Container
                    .SelectMany(x => x.Value)
                    .Where(x => x.Key == tableName)
                    .SelectMany(x => x.Value)
                    .SelectMany(x => x.Rows)
                    .ToList();
            }) as List<Dictionary<string, object>>;
        }

        /// <summary>
        /// Gets values for a specific table and column
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <param name="columnName">Name of the column</param>
        /// <returns>List of values for the specified column</returns>
        public IReadOnlyList<object> GetValues(string tableName, string columnName)
        {
            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(columnName))
                return new List<object>();

            var cacheKey = $"GetValues_{tableName}_{columnName}";
            return _cache.GetOrAdd(cacheKey, _ =>
            {
                return GetValues(tableName).Select(x => x.TryGetValue(columnName, out var value) ? value : null).ToList();
            }) as List<object>;
        }

        /// <summary>
        /// Gets values from JSON name and column
        /// </summary>
        /// <param name="jsonName">JSON name</param>
        /// <param name="columnName">Column name</param>
        /// <param name="schemaController">Schema controller to get table names from JSON</param>
        /// <returns>List of values for the specified column across all tables in JSON</returns>
        public IReadOnlyList<object> GetValuesFromJson(string jsonName, string columnName)
        {
            var schemaController = _context.Completed.Schema;
            if (string.IsNullOrEmpty(jsonName) || string.IsNullOrEmpty(columnName) || schemaController == null)
                return new List<object>();

            var cacheKey = $"GetValuesFromJson_{jsonName}_{columnName}";
            return _cache.GetOrAdd(cacheKey, _ =>
            {
                var tableNames = schemaController.GetTableNamesFromJson(jsonName).ToList();
                return tableNames.SelectMany(tableName =>
                {
                    return GetValues(tableName).Select(x => x.TryGetValue(columnName, out var value) ? value : null);
                }).ToList();
            }) as List<object>;
        }

        /// <summary>
        /// Gets data convert results for a specific table
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <returns>List of data convert results for the table</returns>
        public List<DataConvertResult> GetDataConvertResults(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                return new List<DataConvertResult>();

            var cacheKey = $"GetDataConvertResults_{tableName}";
            return _cache.GetOrAdd(cacheKey, _ =>
            {
                return Container
                    .SelectMany(x => x.Value)
                    .Where(x => x.Key == tableName)
                    .SelectMany(x => x.Value)
                    .ToList();
            }) as List<DataConvertResult>;
        }

        /// <summary>
        /// Gets data for a specific file
        /// </summary>
        /// <param name="fileName">Name of the file</param>
        /// <returns>Dictionary mapping table names to data convert results</returns>
        public Dictionary<string, List<DataConvertResult>> GetDataByFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return new Dictionary<string, List<DataConvertResult>>();

            return Container.TryGetValue(fileName, out var data) ? data : new Dictionary<string, List<DataConvertResult>>();
        }

        /// <summary>
        /// Checks if a table exists in completed data
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
        /// Gets the number of tables in completed data
        /// </summary>
        /// <returns>Number of tables</returns>
        public int GetTableCount()
        {
            return GetAllTableNames().Count;
        }

        /// <summary>
        /// Gets all file names that contain completed data
        /// </summary>
        /// <returns>Set of file names</returns>
        public HashSet<string> GetAllFileNames()
        {
            var key = "GetAllFileNames";
            return _cache.GetOrAdd(key, _ => Container.Keys.ToHashSet()) as HashSet<string>;
        }

        /// <summary>
        /// Validates all completed data for consistency
        /// </summary>
        /// <returns>True if all data is valid</returns>
        public bool ValidateAll()
        {
            try
            {
                // Basic validation - check for null or empty data
                foreach (var (fileName, tableData) in Container)
                {
                    if (string.IsNullOrEmpty(fileName))
                        return false;

                    if (tableData == null)
                        return false;

                    foreach (var (tableName, dataResults) in tableData)
                    {
                        if (string.IsNullOrEmpty(tableName))
                            return false;

                        if (dataResults == null)
                            return false;

                        foreach (var dataResult in dataResults)
                        {
                            if (dataResult == null)
                                return false;

                            if (string.IsNullOrEmpty(dataResult.TableName))
                                return false;

                            if (dataResult.Rows == null)
                                return false;
                        }
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
        /// Gets statistics about completed data
        /// </summary>
        /// <returns>Completed data statistics</returns>
        public CompletedDataStatistics GetStatistics()
        {
            return new CompletedDataStatistics
            {
                FileCount = GetAllFileNames().Count,
                TableCount = GetTableCount(),
                TotalRowCount = Container.SelectMany(x => x.Value)
                    .SelectMany(x => x.Value)
                    .Sum(x => x.Rows.Count)
            };
        }

        /// <summary>
        /// Gets data grouped by sheet name
        /// </summary>
        /// <returns>Dictionary mapping sheet names to table data</returns>
        public Dictionary<string, Dictionary<string, List<DataConvertResult>>> GetDataBySheetName()
        {
            var cacheKey = "GetDataBySheetName";
            return _cache.GetOrAdd(cacheKey, _ =>
            {
                return Container.SelectMany(x => x.Value.SelectMany(x => x.Value))
                    .GroupBy(x => x.SheetName)
                    .ToDictionary(x => x.Key, x =>
                    {
                        return x.GroupBy(x => x.TableName)
                            .ToDictionary(x => x.Key, x => x.ToList());
                    });
            }) as Dictionary<string, Dictionary<string, List<DataConvertResult>>>;
        }

        public bool ContainsKey(string key)
        {
            return Container.ContainsKey(key);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out Dictionary<string, List<DataConvertResult>> value)
        {
            return Container.TryGetValue(key, out value);
        }

        public IEnumerator<KeyValuePair<string, Dictionary<string, List<DataConvertResult>>>> GetEnumerator()
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
                Container = JsonConvert.DeserializeObject<DataContainer>(Encoding.UTF8.GetString(bytes));
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
    /// Statistics information about completed data
    /// </summary>
    public class CompletedDataStatistics
    {
        /// <summary>
        /// Gets or sets the number of files
        /// </summary>
        public int FileCount { get; set; }

        /// <summary>
        /// Gets or sets the number of tables
        /// </summary>
        public int TableCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of rows across all tables
        /// </summary>
        public int TotalRowCount { get; set; }
    }
}