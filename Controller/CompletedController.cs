using ExcelTableConverter.Factory;
using ExcelTableConverter.Model;
using ExcelTableConverter.Worker;
using System.Collections.Concurrent;

namespace ExcelTableConverter.Controller
{
    using ConstContainer = Dictionary<string, Dictionary<string, ConstData>>;
    using DataContainer = Dictionary<string, Dictionary<string, List<DataConvertResult>>>;
    using EnumContainer = Dictionary<string, Dictionary<string, List<object>>>;
    using SchemaContainer = Dictionary<string, SchemaSet>;

    /// <summary>
    /// Manages all completed data containers and provides unified access to completed data operations
    /// Coordinates between CompletedSchemaController, CompletedDataController, CompletedEnumController, and CompletedConstController
    /// </summary>
    public class CompletedController
    {
        private readonly ConcurrentDictionary<object, object> _cache = new ConcurrentDictionary<object, object>();
        private readonly Context _context;

        /// <summary>
        /// Gets the completed schema controller
        /// </summary>
        public CompletedSchemaController Schema { get; }

        /// <summary>
        /// Gets the completed data controller
        /// </summary>
        public CompletedDataController Data { get; }

        /// <summary>
        /// Gets the completed enum controller
        /// </summary>
        public CompletedEnumController Enum { get; }

        /// <summary>
        /// Gets the completed constant controller
        /// </summary>
        public CompletedConstController Const { get; }

        /// <summary>
        /// Gets all table names from completed schema
        /// </summary>
        public HashSet<string> AllTableNames => Schema.GetAllTableNames();

        /// <summary>
        /// Gets table names that have keys defined
        /// </summary>
        public HashSet<string> KeyTableNames => Schema.GetKeyTableNames();

        /// <summary>
        /// Initializes a new instance of the CompletedController class
        /// </summary>
        /// <param name="context">Context instance for configuration and data access</param>
        public CompletedController(Context context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            Schema = new CompletedSchemaController(context);
            Data = new CompletedDataController(context);
            Enum = new CompletedEnumController(context);
            Const = new CompletedConstController(context);
        }

        /// <summary>
        /// Initializes a new instance of the CompletedController class with existing containers
        /// </summary>
        /// <param name="context">Context instance for configuration and data access</param>
        /// <param name="schemaContainer">Completed schema container</param>
        /// <param name="dataContainer">Completed data container</param>
        /// <param name="enumContainer">Completed enum container</param>
        /// <param name="constContainer">Completed const container</param>
        public CompletedController(
            Context context,
            SchemaContainer schemaContainer,
            DataContainer dataContainer,
            EnumContainer enumContainer,
            ConstContainer constContainer)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            Schema = new CompletedSchemaController(context, schemaContainer);
            Data = new CompletedDataController(context, dataContainer);
            Enum = new CompletedEnumController(context, enumContainer);
            Const = new CompletedConstController(context, constContainer);
        }

        /// <summary>
        /// Validates all completed data for consistency and integrity
        /// </summary>
        /// <returns>True if all validations pass</returns>
        public bool ValidateAll()
        {
            var schemaValid = Schema.ValidateAll();
            var dataValid = Data.ValidateAll();
            var enumValid = Enum.ValidateAll();
            var constValid = Const.ValidateAll();

            return schemaValid && dataValid && enumValid && constValid;
        }

        /// <summary>
        /// Gets overall statistics about completed data
        /// </summary>
        /// <returns>Completed data statistics</returns>
        public CompletedStatistics GetStatistics()
        {
            return new CompletedStatistics
            {
                SchemaTableCount = Schema.GetTableCount(),
                DataTableCount = Data.GetTableCount(),
                EnumTableCount = Enum.GetTableCount(),
                ConstTableCount = Const.GetTableCount()
            };
        }

        /// <summary>
        /// Validates cross-references between different completed data types
        /// </summary>
        /// <returns>True if all cross-references are valid</returns>
        public bool ValidateCrossReferences()
        {
            // TODO: Implement cross-reference validation logic
            // For example, validate that data tables exist in schema, enum references are valid, etc.
            return true;
        }

        /// <summary>
        /// Clears all cached values
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
            Schema.ClearCache();
            Data.ClearCache();
            Enum.ClearCache();
            Const.ClearCache();
        }

        /// <summary>
        /// Generates hierarchical data set maintaining inheritance hierarchy for languages that support inheritance
        /// </summary>
        /// <param name="scope">The scope to filter data</param>
        /// <returns>Dictionary mapping JSON names to data containers with hierarchical structure</returns>
        public Dictionary<string, object> GetHierarchicalDataSet(Scope scope)
        {
            // {table:rows}
            var tableRows = Data.Container.SelectMany(x => x.Value).GroupBy(x => x.Key).ToDictionary(x => x.Key, x =>
            {
                var table = x.Key;
                return x.SelectMany(x => x.Value).SelectMany(x => x.Rows).ToList();
            });

            // {json:rows}
            var jsonRows = new Dictionary<string/*json*/, List<Dictionary<string/*column*/, object/*value*/>>>();
            foreach (var g in Schema.Container.GroupBy(x => x.Value.Json))
            {
                var json = g.Key;
                var rows = new List<Dictionary<string, object>>();
                foreach (var tableName in g.Select(x => x.Key))
                {
                    var schema = Schema.Container[tableName].Values.Where(x => x.Scope.HasFlag(scope));
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
                return new DataContainerFactory(scope).Build(_context, json, rows);
            }).Where(pair => pair.Value != null).ToDictionary(x => x.Key, x => x.Value);
        }

        /// <summary>
        /// Flattens inheritance hierarchy by moving inherited fields into the main object.
        /// Converts "Is-A" relationship to "Has-A" relationship for composition-based languages.
        /// </summary>
        /// <param name="tableName">Name of the table to process</param>
        /// <param name="row">Row data to convert</param>
        /// <returns>Converted row with composition structure</returns>
        private Dictionary<string, object> FlattenInheritanceStructure(string tableName, Dictionary<string, object> row)
        {
            var based = Schema[tableName].Based;
            if (based == null)
                return row;

            var inheritedFields = Schema[tableName].Where(x => x.Value.Inherited).ToDictionary(x => x.Key, x => x.Value);
            var inheritedValues = new Dictionary<string, object>();
            row = row.ToDictionary(x => x.Key, x => x.Value);
            foreach (var k in inheritedFields.Keys)
            {
                inheritedValues.Add(k, row[k]);
                row.Remove(k);
            }
            row[based] = FlattenInheritanceStructure(based, inheritedValues);
            return row;
        }

        private Dictionary<string, object> FilterScope(string tableName, Scope scope, Dictionary<string, object> row)
        {
            var result = new Dictionary<string, object>();
            var schema = Schema[tableName].Values.Where(x => x.Scope.HasFlag(scope));
            var columns = schema.Where(x => !x.Inherited).Select(x => x.Name).ToHashSet();

            foreach (var column in columns)
            {
                result.Add(column, row[column]);
            }

            var based = Schema[tableName].Based;
            if (based != null)
            {
                result[based] = FilterScope(based, scope, row[based] as Dictionary<string, object>);
            }

            return row;
        }

        /// <summary>
        /// Generates data set with flattened structure for languages without inheritance support.
        /// Converts inheritance relationships to composition for Go code generation.
        /// Also applies enum string-to-integer conversion.
        /// </summary>
        /// <param name="scope">The scope to filter data</param>
        /// <returns>Dictionary mapping JSON names to data containers with flattened structure</returns>
        public Dictionary<string, object> GetFlattenedDataSet(Scope scope)
        {
            // {table:rows}
            var tableRows = Data.SelectMany(x => x.Value).GroupBy(x => x.Key).ToDictionary(x => x.Key, x =>
            {
                var table = x.Key;
                return x.SelectMany(x => x.Value).SelectMany(x => x.Rows).ToList();
            });

            // enum value(string) to integer
            foreach (var (tableName, rows) in tableRows)
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    foreach (var (k, v) in row)
                    {
                        var schema = Schema[tableName][k];
                        var naked = Util.Type.Nake(schema.Type);
                        if (Enum.ContainsKey(naked))
                        {
                            if (v != null && v is string)
                            {
                                row[k] = Enum.ConvertToInt(naked, v);
                            }
                        }
                    }

                    rows[i] = FlattenInheritanceStructure(tableName, row);
                }
            }

            // {json:rows}
            var jsonRows = new Dictionary<string/*json*/, List<Dictionary<string/*column*/, object/*value*/>>>();
            foreach (var g in Schema.GroupBy(x => x.Value.Json))
            {
                var json = g.Key;
                var rows = new List<Dictionary<string, object>>();
                foreach (var tableName in g.Select(x => x.Key))
                {
                    var schema = Schema[tableName].Values.Where(x => x.Scope.HasFlag(scope));
                    var columns = schema.Select(x => x.Name).ToHashSet();
                    if (columns.Count == 0)
                        continue;

                    foreach (var row in tableRows[tableName])
                    {
                        rows.Add(FilterScope(tableName, scope, row));
                    }
                }

                jsonRows.Add(json, rows);
            }

            return jsonRows.ToDictionary(x => x.Key, x =>
            {
                var json = x.Key;
                var rows = x.Value;
                return new DataContainerFactory(scope).Build(_context, json, rows);
            }).Where(pair => pair.Value != null).ToDictionary(x => x.Key, x => x.Value);
        }

        /// <summary>
        /// Generates hierarchical data set grouped by sheet name
        /// </summary>
        /// <param name="scope">The scope to filter data</param>
        /// <returns>Dictionary mapping sheet names to table containers with hierarchical structure</returns>
        public Dictionary<string, Dictionary<string, object>> GetHierarchicalDataSetWithSheetName(Scope scope)
        {
            return Data.Container.SelectMany(x => x.Value.SelectMany(x => x.Value)).GroupBy(x => x.SheetName).ToDictionary(x => x.Key, x =>
            {
                return x.GroupBy(x => x.TableName).ToDictionary(x => x.Key, x =>
                {
                    var tableName = x.Key;
                    var schema = Schema.Container[tableName].Values.Where(x => x.Scope.HasFlag(scope));
                    var columns = schema.Select(x => x.Name).ToHashSet();
                    if (columns.Count == 0)
                        return null;

                    var rows = x.SelectMany(x => x.Rows)
                        .Select(row => row.Where(x => columns.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value))
                        .Where(x => x.Count > 0)
                        .ToList();

                    return new DataContainerFactory(scope).Build(_context, x.Key, rows);
                }).Where(pair => pair.Value != null).ToDictionary(x => x.Key, x => x.Value);
            });
        }

        /// <summary>
        /// Merges another CompletedController into this one
        /// </summary>
        /// <param name="other">Other CompletedController to merge</param>
        /// <returns>New CompletedController with merged data</returns>
        public CompletedController Merge(CompletedController other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            var mergedSchema = Schema.Container.Concat(other.Schema.Container).ToDictionary(x => x.Key, x => x.Value);
            var mergedData = Data.Container.Concat(other.Data.Container).ToDictionary(x => x.Key, x => x.Value);
            var mergedEnum = Enum.Container.Concat(other.Enum.Container).ToDictionary(x => x.Key, x => x.Value);
            var mergedConst = Const.Container.Concat(other.Const.Container).ToDictionary(x => x.Key, x => x.Value);

            return new CompletedController(_context, mergedSchema, mergedData, mergedEnum, mergedConst);
        }

        public byte[] ToBytes()
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            var schemaBytes = Schema.ToBytes();
            var dataBytes = Data.ToBytes();
            var enumBytes = Enum.ToBytes();
            var constBytes = Const.ToBytes();

            writer.Write(schemaBytes.Length);
            writer.Write(schemaBytes);
            writer.Write(dataBytes.Length);
            writer.Write(dataBytes);
            writer.Write(enumBytes.Length);
            writer.Write(enumBytes);
            writer.Write(constBytes.Length);
            writer.Write(constBytes);

            return ms.ToArray();
        }

        public bool FromBytes(byte[] bytes)
        {
            try
            {
                using var ms = new MemoryStream(bytes);
                using var reader = new BinaryReader(ms);

                var schemaBytes = reader.ReadBytes(reader.ReadInt32());
                Schema.FromBytes(schemaBytes);

                var dataBytes = reader.ReadBytes(reader.ReadInt32());
                Data.FromBytes(dataBytes);

                var enumBytes = reader.ReadBytes(reader.ReadInt32());
                Enum.FromBytes(enumBytes);

                var constBytes = reader.ReadBytes(reader.ReadInt32());
                Const.FromBytes(constBytes);

                return true;
            }
            catch (Exception)
            {
                Clear();
                return false;
            }
        }

        public void Clear()
        {
            Schema.Clear();
            Data.Clear();
            Enum.Clear();
            Const.Clear();
        }
    }

    /// <summary>
    /// Statistics information about completed data
    /// </summary>
    public class CompletedStatistics
    {
        /// <summary>
        /// Gets or sets the number of schema tables
        /// </summary>
        public int SchemaTableCount { get; set; }

        /// <summary>
        /// Gets or sets the number of data tables
        /// </summary>
        public int DataTableCount { get; set; }

        /// <summary>
        /// Gets or sets the number of enum tables
        /// </summary>
        public int EnumTableCount { get; set; }

        /// <summary>
        /// Gets or sets the number of const tables
        /// </summary>
        public int ConstTableCount { get; set; }

        /// <summary>
        /// Gets the total number of tables across all types
        /// </summary>
        public int TotalTableCount => SchemaTableCount + DataTableCount + EnumTableCount + ConstTableCount;
    }
}