using ExcelTableConverter.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ExcelTableConverter.Controller
{
    using EnumContainer = Dictionary<string, Dictionary<string, List<object>>>;

    /// <summary>
    /// Manages completed enum operations and provides access to enum data
    /// Handles enum value operations, conversions, and validation for completed data
    /// </summary>
    public class CompletedEnumController : IReadOnlyDictionary<string, Dictionary<string, List<object>>>
    {
        private readonly ConcurrentDictionary<object, object> _cache = new();
        private readonly Context _context;

        /// <summary>
        /// Gets the completed enum container
        /// </summary>
        public EnumContainer Container { get; private set; }

        public IEnumerable<string> Keys => Container.Keys;

        public IEnumerable<Dictionary<string, List<object>>> Values => Container.Values;

        public int Count => Container.Count;

        public Dictionary<string, List<object>> this[string key] => Container[key];

        /// <summary>
        /// Initializes a new instance of the CompletedEnumController class
        /// </summary>
        /// <param name="context">Context instance for configuration and data access</param>
        public CompletedEnumController(Context context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            Container = new EnumContainer();
        }

        /// <summary>
        /// Initializes a new instance of the CompletedEnumController class with existing container
        /// </summary>
        /// <param name="context">Context instance for configuration and data access</param>
        /// <param name="container">Existing enum container</param>
        public CompletedEnumController(Context context, EnumContainer container)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            Container = container ?? new EnumContainer();
        }

        /// <summary>
        /// Updates the enum container with new data
        /// </summary>
        /// <param name="newContainer">New enum container to replace current one</param>
        public void UpdateContainer(EnumContainer newContainer)
        {
            Container = newContainer ?? new EnumContainer();
            ClearCache(); // Clear cache when container is updated
        }

        /// <summary>
        /// Builds and updates enum data from source enum controller
        /// </summary>
        /// <param name="sourceEnumController">Source enum controller</param>
        /// <param name="dslTypeEnumName">DSL type enum name</param>
        /// <param name="dsl">DSL object for function types</param>
        public void BuildFromSourceData(SourceEnumController sourceEnumController, string dslTypeEnumName, JObject dsl)
        {
            if (sourceEnumController == null)
                throw new ArgumentNullException(nameof(sourceEnumController));

            var enumContainer = sourceEnumController.GetAllEnums().GroupBy(x => x.Table)
                .ToDictionary(x => x.Key, x => x.SelectMany(x => x.Values).ToDictionary(x => x.Key, x => x.Value));

            // Add DSL function types if provided
            if (!string.IsNullOrEmpty(dslTypeEnumName) && dsl != null)
            {
                var dslFunctionTypes = new Dictionary<string, List<object>>();
                enumContainer.Add(dslTypeEnumName, dslFunctionTypes);
                int i = 0;
                foreach (var dslItem in dsl)
                {
                    dslFunctionTypes.Add(dslItem.Key, [i++]);
                }
            }

            UpdateContainer(enumContainer);
        }

        /// <summary>
        /// Gets all enum type names from completed data
        /// </summary>
        /// <returns>Set of all enum type names</returns>
        public HashSet<string> GetAllEnumTypes()
        {
            var key = "GetAllEnumTypes";
            return _cache.GetOrAdd(key, _ => Container.Keys.ToHashSet()) as HashSet<string>;
        }

        /// <summary>
        /// Gets enum values for a specific enum type
        /// </summary>
        /// <param name="enumType">Name of the enum type</param>
        /// <returns>Dictionary of enum values, or null if not found</returns>
        public Dictionary<string, List<object>> GetEnumValues(string enumType)
        {
            if (string.IsNullOrEmpty(enumType))
                return null;

            return Container.TryGetValue(enumType, out var values) ? values : null;
        }

        /// <summary>
        /// Gets a specific enum value
        /// </summary>
        /// <param name="enumType">Name of the enum type</param>
        /// <param name="valueName">Name of the enum value</param>
        /// <returns>List of objects for the enum value, or null if not found</returns>
        public List<object> GetEnumValue(string enumType, string valueName)
        {
            if (string.IsNullOrEmpty(enumType) || string.IsNullOrEmpty(valueName))
                return null;

            var cacheKey = $"GetEnumValue_{enumType}_{valueName}";
            return _cache.GetOrAdd(cacheKey, _ =>
            {
                if (!Container.TryGetValue(enumType, out var enumValues))
                    return null;

                return enumValues.TryGetValue(valueName, out var value) ? value : null;
            }) as List<object>;
        }

        /// <summary>
        /// Converts enum value to integer
        /// </summary>
        /// <param name="enumType">Name of the enum type</param>
        /// <param name="value">Value to convert</param>
        /// <returns>Integer representation of the enum value</returns>
        public int EnumValueToInt(string enumType, object value)
        {
            if (value is int i)
                return i;

            var s = value as string;
            if (string.IsNullOrEmpty(s))
                throw new ArgumentException($"Invalid enum value: {value}");

            if (!Container.TryGetValue(enumType, out var enumValues))
                throw new ArgumentException($"Enum type not found: {enumType}");

            if (enumValues.TryGetValue(s, out var enumValue))
            {
                if (enumValue.Count != 1)
                    throw new InvalidOperationException($"Enum value {s} has invalid format");

                s = enumValue[0] as string;
            }

            if (s.StartsWith("0x"))
                return Convert.ToInt32(s, 16);

            return int.Parse(s);
        }

        /// <summary>
        /// Checks if an enum type exists
        /// </summary>
        /// <param name="enumType">Name of the enum type to check</param>
        /// <returns>True if enum type exists</returns>
        public bool ContainsEnumType(string enumType)
        {
            if (string.IsNullOrEmpty(enumType))
                return false;

            return Container.ContainsKey(enumType);
        }

        /// <summary>
        /// Checks if an enum value exists
        /// </summary>
        /// <param name="enumType">Name of the enum type</param>
        /// <param name="valueName">Name of the enum value</param>
        /// <returns>True if enum value exists</returns>
        public bool ContainsEnumValue(string enumType, string valueName)
        {
            if (string.IsNullOrEmpty(enumType) || string.IsNullOrEmpty(valueName))
                return false;

            if (!Container.TryGetValue(enumType, out var enumValues))
                return false;

            return enumValues.ContainsKey(valueName);
        }

        /// <summary>
        /// Gets the number of enum types
        /// </summary>
        /// <returns>Number of enum types</returns>
        public int GetTableCount()
        {
            return Container.Count;
        }

        /// <summary>
        /// Gets all enum value names for a specific enum type
        /// </summary>
        /// <param name="enumType">Name of the enum type</param>
        /// <returns>Set of enum value names</returns>
        public HashSet<string> GetEnumValueNames(string enumType)
        {
            if (string.IsNullOrEmpty(enumType))
                return new HashSet<string>();

            var cacheKey = $"GetEnumValueNames_{enumType}";
            return _cache.GetOrAdd(cacheKey, _ =>
            {
                if (!Container.TryGetValue(enumType, out var enumValues))
                    return new HashSet<string>();

                return enumValues.Keys.ToHashSet();
            }) as HashSet<string>;
        }

        public int ConvertToInt(string enumType, object value)
        {
            if (value is int i)
                return i;

            var s = value as string;
            if (GetEnumValues(enumType)?.TryGetValue(s, out var x) == true)
            {
                if (x.Count != 1)
                    throw new LogicException("...?");

                s = x[0] as string;
            }

            if (s.StartsWith("0x"))
                return Convert.ToInt32(s, 16);

            return int.Parse(s);
        }

        /// <summary>
        /// Validates all completed enum data for consistency
        /// </summary>
        /// <returns>True if all data is valid</returns>
        public bool ValidateAll()
        {
            try
            {
                // Basic validation - check for null or empty data
                foreach (var (enumType, enumValues) in Container)
                {
                    if (string.IsNullOrEmpty(enumType))
                        return false;

                    if (enumValues == null)
                        return false;

                    foreach (var (valueName, valueList) in enumValues)
                    {
                        if (string.IsNullOrEmpty(valueName))
                            return false;

                        if (valueList == null)
                            return false;

                        // Check that all values in the list are valid
                        foreach (var value in valueList)
                        {
                            if (value == null)
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
        /// Gets statistics about completed enum data
        /// </summary>
        /// <returns>Completed enum statistics</returns>
        public CompletedEnumStatistics GetStatistics()
        {
            return new CompletedEnumStatistics
            {
                EnumTypeCount = GetTableCount(),
                TotalEnumValueCount = Container.Values.Sum(x => x.Count)
            };
        }

        /// <summary>
        /// Gets all enum data flattened into a single dictionary
        /// </summary>
        /// <returns>Dictionary mapping enum type and value names to their values</returns>
        public Dictionary<string, object> GetFlattenedEnumData()
        {
            var cacheKey = "GetFlattenedEnumData";
            return _cache.GetOrAdd(cacheKey, _ =>
            {
                var result = new Dictionary<string, object>();
                foreach (var (enumType, enumValues) in Container)
                {
                    foreach (var (valueName, valueList) in enumValues)
                    {
                        var key = $"{enumType}.{valueName}";
                        result[key] = valueList.Count == 1 ? valueList[0] : valueList;
                    }
                }
                return result;
            }) as Dictionary<string, object>;
        }

        public bool ContainsKey(string key)
        {
            return Container.ContainsKey(key);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out Dictionary<string, List<object>> value)
        {
            return Container.TryGetValue(key, out value);
        }

        public IEnumerator<KeyValuePair<string, Dictionary<string, List<object>>>> GetEnumerator()
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
                Container = JsonConvert.DeserializeObject<EnumContainer>(Encoding.UTF8.GetString(bytes));
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
    /// Statistics information about completed enum data
    /// </summary>
    public class CompletedEnumStatistics
    {
        /// <summary>
        /// Gets or sets the number of enum types
        /// </summary>
        public int EnumTypeCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of enum values across all types
        /// </summary>
        public int TotalEnumValueCount { get; set; }
    }
}