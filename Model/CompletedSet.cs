using ExcelTableConverter.Configuration;
using ExcelTableConverter.Factory;
using ExcelTableConverter.Worker;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace ExcelTableConverter.Model
{
    public class SchemaMap : Dictionary<string, SchemaSet>
    {
        private readonly ConcurrentDictionary<string, SchemaData> _keys = new();
        private readonly ConcurrentDictionary<string, int> _inheritanceLevels = new();

        public HashSet<string> KeyTableNames => Keys.Where(x => FindKey(x) != null).ToHashSet();

        public HashSet<string> BaseTables => Values.Where(x => string.IsNullOrEmpty(x.Based) == false).Select(x => x.Based).ToHashSet();

        public static SchemaMap Build(SourceDataMap source, AppConfiguration configuration)
        {
            var result = new SchemaMap();
            var group = source.Values.SelectMany(x => x).GroupBy(x => x.TableName).ToDictionary(x => x.Key, x => x.ToList());

            foreach (var (table, sheets) in group)
            {
                var based = sheets[0].Based;
                var json = sheets[0].Json;
                var (boldColumns, normalColumns) = sheets[0].Columns.Split();
                var root = string.Format(configuration.ParentTableFormat, table);

                if (boldColumns != null)
                {
                    var schemaSet = new SchemaSet(null, root);
                    foreach (var column in boldColumns)
                        schemaSet.Add(column.Name, new SchemaData { Name = column.Name, Type = column.Type, Scope = column.Scope });

                    result.Add(root, schemaSet);
                }

                if (normalColumns != null)
                {
                    var schemaSet = new SchemaSet(based, json);
                    if (boldColumns != null)
                    {
                        var parentKeyColumn = boldColumns.FirstOrDefault(x => x.ColumnType.PrimaryKey);
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

            return result;
        }

        public SchemaData FindKey(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                return null;

            return _keys.GetOrAdd(tableName, _ =>
            {
                if (TryGetValue(tableName, out var schemaSet) == false)
                    return null;

                return schemaSet.Values.FirstOrDefault(x => x.ColumnType.GroupKey) ??
                       schemaSet.Values.FirstOrDefault(x => x.ColumnType.PrimaryKey);
            });
        }

        public bool ContainsColumn(string tableName, string columnName)
        {
            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(columnName))
                return false;

            return TryGetValue(tableName, out var schemaSet) && schemaSet.ContainsKey(columnName);
        }

        public int InheritanceLevel(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                return 0;

            return _inheritanceLevels.GetOrAdd(tableName, _ =>
            {
                if (TryGetValue(tableName, out var schemaSet) == false || string.IsNullOrEmpty(schemaSet.Based))
                    return 0;

                return 1 + InheritanceLevel(schemaSet.Based);
            });
        }

        public string RootType(string type, bool recursion = true)
        {
            var column = ColumnType.Parse(type);
            if (column.Relation != null)
            {
                var naked = column.Relation;

                if (naked.Contains("."))
                {
                    var split = naked.Split(".");
                    naked = split[0];
                    var refer = split[1];

                    if (TryGetValue(naked, out var schemaSet) == false)
                        throw new LogicException($"{naked} 테이블은 정의되지 않았습니다.");

                    if (schemaSet.TryGetValue(refer, out var x) == false)
                        throw new LogicException($"{refer}는 {naked} 테이블에 정의되지 않았습니다.");

                    type = x.ColumnType.Unkeyed;
                }
                else
                {
                    if (TryGetValue(naked, out var schemaSet) == false)
                        throw new LogicException($"{naked} 테이블은 정의되지 않았습니다.");

                    var key = schemaSet.Key;
                    if (key == null)
                        throw new LogicException($"{naked} 테이블은 키 정의가 되지 않았습니다.");

                    type = schemaSet[key].ColumnType.Unkeyed;
                }

                if (recursion)
                    type = RootType(type, recursion);

                if (column.Nullable && ColumnType.Parse(type).Nullable == false)
                    type = $"{type}?";

                return type;
            }
            else if (column.Sequence)
            {
                return column.Nullable ? "int?" : "int";
            }
            else
            {
                return column.Value;
            }
        }
    }

    public class DataMap : Dictionary<string, Dictionary<string, List<DataConvertResult>>>
    {
        private readonly ConcurrentDictionary<string, object> _values = new();

        public HashSet<string> TableNames => Values.SelectMany(x => x.Keys).ToHashSet();

        public static DataMap Build(IEnumerable<DataConvertResult> results)
        {
            var result = new DataMap();
            foreach (var file in results.GroupBy(x => x.FileName))
                result.Add(file.Key, file.GroupBy(x => x.TableName).ToDictionary(x => x.Key, x => x.OrderBy(x => x.SheetName).ToList()));

            return result;
        }

        public List<Dictionary<string, DataValue>> Rows(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                return new List<Dictionary<string, DataValue>>();

            return _values.GetOrAdd(tableName, _ =>
            {
                return Values
                    .SelectMany(x => x)
                    .Where(x => x.Key == tableName)
                    .SelectMany(x => x.Value)
                    .SelectMany(x => x.Rows)
                    .ToList();
            }) as List<Dictionary<string, DataValue>>;
        }

        public IReadOnlyList<DataValue> Column(string tableName, string columnName)
        {
            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(columnName))
                return new List<DataValue>();

            return _values.GetOrAdd($"{tableName}.{columnName}", _ =>
            {
                return Rows(tableName).Select(x => x.TryGetValue(columnName, out var value) ? value : null).ToList();
            }) as List<DataValue>;
        }

        public IReadOnlyList<DataValue> JsonColumn(SchemaMap schema, string json, string columnName)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(columnName))
                return new List<DataValue>();

            return _values.GetOrAdd($"json:{json}.{columnName}", _ =>
            {
                return schema.Where(x => x.Value.Json == json).SelectMany(x =>
                {
                    return Rows(x.Key).Select(x => x.TryGetValue(columnName, out var value) ? value : null);
                }).ToList();
            }) as List<DataValue>;
        }
    }

    public class EnumMap : Dictionary<string, Dictionary<string, EnumExpression>>
    {
        public static EnumMap Build(SourceEnumMap source, string dslTypeEnumName, JObject dsl)
        {
            var result = new EnumMap();
            foreach (var table in source.Values.SelectMany(x => x).GroupBy(x => x.Table))
                result.Add(table.Key, table.SelectMany(x => x.Values).ToDictionary(x => x.Key, x => x.Value));

            if (string.IsNullOrEmpty(dslTypeEnumName) == false && dsl != null)
            {
                var dslFunctionTypes = new Dictionary<string, EnumExpression>();
                result.Add(dslTypeEnumName, dslFunctionTypes);
                var i = 0;
                foreach (var dslItem in dsl)
                    dslFunctionTypes.Add(dslItem.Key, EnumExpression.Parse($"{i++}"));
            }

            return result;
        }

        // A member defined by a single number, or a number literal.
        public int Number(string enumType, string name)
        {
            var s = name;
            if (TryGetValue(enumType, out var members) && members.TryGetValue(name, out var expression))
            {
                if (expression.Items.Count != 1 || expression.Items[0].Token == null)
                    throw new LogicException($"{enumType}.{name}은 숫자로 정의된 열거형이 아닙니다.");

                s = expression.Items[0].Token;
            }

            if (s.StartsWith("0x"))
                return Convert.ToInt32(s, 16);

            return int.Parse(s);
        }
    }

    public class ConstMap : Dictionary<string, Dictionary<string, ConstData>>
    {
        public static ConstMap Build(SourceConstMap source, Func<string, object, IExcelFileTrackable, DataValue> cast)
        {
            var result = new ConstMap();
            foreach (var table in source.Values.SelectMany(x => x).GroupBy(x => x.TableName))
            {
                result.Add(table.Key, table.OrderBy(x => x.FileName).ThenBy(x => x.SheetName).ToDictionary(x => x.Name, x => new ConstData
                {
                    Name = x.Name,
                    Type = x.Type,
                    Scope = x.Scope,
                    Value = cast(x.Type, x.Value, null)
                }));
            }

            return result;
        }
    }

    public class CompletedSet
    {
        private readonly Context _context;

        public SchemaMap Schema { get; private set; } = new();
        public DataMap Data { get; private set; } = new();
        public EnumMap Enum { get; private set; } = new();
        public ConstMap Const { get; private set; } = new();

        public CompletedSet(Context context)
        {
            _context = context;
        }

        public void Build(SourceSet source, AppConfiguration configuration, JObject dsl)
        {
            Enum = EnumMap.Build(source.Enum, configuration.DslTypeEnumName, dsl);
            Schema = SchemaMap.Build(source.Data, configuration);
            Data = DataMap.Build(new DataTypeCaster(_context).Run());
            Const = ConstMap.Build(source.Const, _context.Cast);
        }

        public Dictionary<string, object> GetHierarchicalDataSet(uint scope)
        {
            var tableRows = Data.SelectMany(x => x.Value).GroupBy(x => x.Key).ToDictionary(x => x.Key, x =>
            {
                return x.SelectMany(x => x.Value).SelectMany(x => x.Rows).ToList();
            });

            var jsonRows = new Dictionary<string, List<Dictionary<string, DataValue>>>();
            foreach (var g in Schema.GroupBy(x => x.Value.Json))
            {
                var rows = new List<Dictionary<string, DataValue>>();
                foreach (var tableName in g.Select(x => x.Key))
                {
                    var columns = Schema[tableName].Values.Where(x => AppConfiguration.ContainsScope(x.Scope, scope)).Select(x => x.Name).ToHashSet();
                    if (columns.Count == 0)
                        continue;

                    rows.AddRange(tableRows[tableName]
                        .Select(row => row.Where(x => columns.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value))
                        .Where(x => x.Count > 0));
                }

                jsonRows.Add(g.Key, rows);
            }

            return jsonRows
                .ToDictionary(x => x.Key, x => new DataContainerFactory(scope).Build(_context, x.Key, x.Value))
                .Where(pair => pair.Value != null)
                .ToDictionary(x => x.Key, x => x.Value);
        }

        private Dictionary<string, DataValue> FlattenInheritanceStructure(string tableName, Dictionary<string, DataValue> row)
        {
            var based = Schema[tableName].Based;
            if (based == null)
                return row;

            var inheritedFields = Schema[tableName].Where(x => x.Value.Inherited).Select(x => x.Key);
            var inheritedValues = new Dictionary<string, DataValue>();
            row = row.ToDictionary(x => x.Key, x => x.Value);
            foreach (var k in inheritedFields)
            {
                inheritedValues.Add(k, row[k]);
                row.Remove(k);
            }
            row[based] = new ObjectValue(FlattenInheritanceStructure(based, inheritedValues));
            return row;
        }

        private Dictionary<string, DataValue> FilterScope(string tableName, uint scope, IReadOnlyDictionary<string, DataValue> row)
        {
            var columns = Schema[tableName].Values.Where(x => AppConfiguration.ContainsScope(x.Scope, scope) && x.Inherited == false).Select(x => x.Name).ToHashSet();
            var based = Schema[tableName].Based;
            var result = new Dictionary<string, DataValue>();
            foreach (var (k, v) in row)
            {
                if (k == based)
                    result.Add(k, new ObjectValue(FilterScope(based, scope, ((ObjectValue)v).Fields)));
                else if (columns.Contains(k))
                    result.Add(k, v);
            }

            return result;
        }

        public Dictionary<string, object> GetFlattenedDataSet(uint scope)
        {
            var tableRows = Data.SelectMany(x => x.Value).GroupBy(x => x.Key).ToDictionary(x => x.Key, x =>
            {
                return x.SelectMany(x => x.Value).SelectMany(x => x.Rows).ToList();
            });

            foreach (var (tableName, rows) in tableRows)
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    var row = new Dictionary<string, DataValue>(rows[i]);
                    foreach (var (k, v) in rows[i])
                    {
                        var naked = Schema[tableName][k].ColumnType.Naked;
                        if (Enum.ContainsKey(naked) && v is EnumValue { Name: not null } e)
                            row[k] = new IntegerValue(Enum.Number(naked, e.Name));
                    }

                    rows[i] = FlattenInheritanceStructure(tableName, row);
                }
            }

            var jsonRows = new Dictionary<string, List<Dictionary<string, DataValue>>>();
            foreach (var g in Schema.GroupBy(x => x.Value.Json))
            {
                var rows = new List<Dictionary<string, DataValue>>();
                foreach (var tableName in g.Select(x => x.Key))
                {
                    var columns = Schema[tableName].Values.Where(x => AppConfiguration.ContainsScope(x.Scope, scope)).Select(x => x.Name).ToHashSet();
                    if (columns.Count == 0)
                        continue;

                    foreach (var row in tableRows[tableName])
                        rows.Add(FilterScope(tableName, scope, row));
                }

                jsonRows.Add(g.Key, rows);
            }

            return jsonRows
                .ToDictionary(x => x.Key, x => new DataContainerFactory(scope).Build(_context, x.Key, x.Value))
                .Where(pair => pair.Value != null)
                .ToDictionary(x => x.Key, x => x.Value);
        }
    }
}
