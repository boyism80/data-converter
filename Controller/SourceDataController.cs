using ExcelTableConverter.Model;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ExcelTableConverter.Controller
{
    using SourceDataContainer = Dictionary<string, List<SourceSheetData>>;

    /// <summary>
    /// Manages source data operations and provides access to source data
    /// Handles data retrieval, sheet operations, and validation for source data
    /// </summary>
    public class SourceDataController : IReadOnlyDictionary<string, List<SourceSheetData>>
    {
        private readonly ConcurrentDictionary<object, object> _cache = new();
        private readonly Context _context;

        /// <summary>
        /// Gets the source data container
        /// </summary>
        public SourceDataContainer Container { get; private set; }

        public IEnumerable<string> Keys => Container.Keys;

        public IEnumerable<List<SourceSheetData>> Values => Container.Values;

        public int Count => Container.Count;

        public List<SourceSheetData> this[string key] => Container[key];

        /// <summary>
        /// Initializes a new instance of the SourceDataController class
        /// </summary>
        /// <param name="context">Context instance for configuration and data access</param>
        public SourceDataController(Context context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            Container = new SourceDataContainer();
        }

        /// <summary>
        /// Initializes a new instance of the SourceDataController class with existing container
        /// </summary>
        /// <param name="context">Context instance for configuration and data access</param>
        /// <param name="container">Existing source data container</param>
        public SourceDataController(Context context, SourceDataContainer container)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            Container = container ?? new SourceDataContainer();
        }

        /// <summary>
        /// Gets all table names from source data
        /// </summary>
        /// <returns>Set of all table names</returns>
        public HashSet<string> GetAllTableNames()
        {
            var key = "GetAllTableNames";
            return _cache.GetOrAdd(key, _ =>
            {
                return Container.SelectMany(x => x.Value).Select(x => x.TableName).ToHashSet();
            }) as HashSet<string>;
        }

        /// <summary>
        /// Gets all sheet data from all files
        /// </summary>
        /// <returns>Enumerable of all sheet data</returns>
        public IEnumerable<SourceSheetData> GetAllSheetData()
        {
            return Container.SelectMany(x => x.Value);
        }

        /// <summary>
        /// Gets columns for a specific table
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <returns>List of columns for the table, or null if table not found</returns>
        public List<SourceDataColumns> GetColumns(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                return null;

            var key = $"GetColumns_{tableName}";
            return _cache.GetOrAdd(key, _ =>
            {
                return Container.SelectMany(x => x.Value)
                    .Where(x => x.TableName == tableName)
                    .FirstOrDefault()?.Columns;
            }) as List<SourceDataColumns>;
        }

        /// <summary>
        /// Finds the sheet data that contains the specified column
        /// </summary>
        /// <param name="column">Column to search for</param>
        /// <returns>Sheet data containing the column, or null if not found</returns>
        public SourceSheetData FindSheetData(SourceDataColumns column)
        {
            if (column == null)
                return null;

            return Container.SelectMany(x => x.Value)
                .FirstOrDefault(x => x.Columns.Contains(column));
        }

        /// <summary>
        /// Gets all sheet data for a specific table
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <returns>List of sheet data for the table</returns>
        public List<SourceSheetData> GetSheetData(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                return new List<SourceSheetData>();

            var key = $"GetSheetData_{tableName}";
            return _cache.GetOrAdd(key, _ =>
            {
                return Container.SelectMany(x => x.Value)
                    .Where(x => x.TableName == tableName)
                    .ToList();
            }) as List<SourceSheetData>;
        }

        /// <summary>
        /// Checks if a table exists in source data
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
        /// Gets the number of tables in source data
        /// </summary>
        /// <returns>Number of tables</returns>
        public int GetTableCount()
        {
            return GetAllTableNames().Count;
        }

        /// <summary>
        /// Gets all file names that contain source data
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
        /// Gets source data for a specific file
        /// </summary>
        /// <param name="fileName">Name of the file</param>
        /// <returns>List of sheet data for the file</returns>
        public List<SourceSheetData> GetDataByFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return new List<SourceSheetData>();

            return Container.TryGetValue(fileName, out var data) ? data : new List<SourceSheetData>();
        }

        /// <summary>
        /// Validates all source data for consistency
        /// </summary>
        /// <returns>True if all data is valid</returns>
        public bool ValidateAll()
        {
            try
            {
                // Basic validation - check for null or empty data
                foreach (var (fileName, sheetDataList) in Container)
                {
                    if (string.IsNullOrEmpty(fileName))
                        return false;

                    if (sheetDataList == null)
                        return false;

                    foreach (var sheetData in sheetDataList)
                    {
                        if (sheetData == null)
                            return false;

                        if (string.IsNullOrEmpty(sheetData.TableName))
                            return false;

                        if (sheetData.Columns == null)
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
        /// Gets source columns for a specific table name
        /// </summary>
        /// <param name="tableName">The table name to search for</param>
        /// <returns>List of source data columns for the table, or null if not found</returns>
        public List<SourceDataColumns> GetSourceColumns(string tableName)
        {
            return GetAllSheetData()
                .Where(x => x.TableName == tableName)
                .FirstOrDefault()?.Columns;
        }

        /// <summary>
        /// Finds the source sheet data that contains the specified column
        /// </summary>
        /// <param name="column">The column to search for</param>
        /// <returns>The source sheet data containing the column, or null if not found</returns>
        public SourceSheetData FindSourceSheetData(SourceDataColumns column)
        {
            return GetAllSheetData().FirstOrDefault(x => x.Columns.Contains(column));
        }

        /// <summary>
        /// Gets statistics about source data
        /// </summary>
        /// <returns>Source data statistics</returns>
        public SourceDataStatistics GetStatistics()
        {
            return new SourceDataStatistics
            {
                FileCount = GetAllFileNames().Count,
                TableCount = GetTableCount(),
                TotalSheetCount = Container.SelectMany(x => x.Value).Count()
            };
        }

        public bool ContainsKey(string key)
        {
            return Container.ContainsKey(key);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out List<SourceSheetData> value)
        {
            return Container.TryGetValue(key, out value);
        }

        public IEnumerator<KeyValuePair<string, List<SourceSheetData>>> GetEnumerator()
        {
            return Container.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Container.GetEnumerator();
        }

        public void Add(string key, List<SourceSheetData> value)
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
                Container = JsonConvert.DeserializeObject<SourceDataContainer>(Encoding.UTF8.GetString(bytes));
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
    /// Statistics information about source data
    /// </summary>
    public class SourceDataStatistics
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
        /// Gets or sets the total number of sheets
        /// </summary>
        public int TotalSheetCount { get; set; }
    }
}