using Newtonsoft.Json;

namespace ExcelTableConverter.Model
{
    public class SourceValue : IExcelFileTrackable
    {
        private string _root, _sheetName, _tableName, _fileName;

        public int Column { get; set; }
        public object Value { get; set; }
        public string Root
        {
            get => Parent?.FullName ?? _root;
            set => _root = value;
        }
        public string SheetName
        {
            get => Parent?.SheetName ?? _sheetName;
            set => _sheetName = value;
        }
        public string TableName
        {
            get => Parent?.Name ?? _tableName;
            set => _tableName = value;
        }

        public string FileName
        {
            get => Parent?.FileName ?? _fileName;
            set => _fileName = value;
        }

        [JsonIgnore] public Sheet Parent { get; set; }
    }

    public class SourceSchemaData
    {
        public string Name { get; set; }
        public uint Scope { get; set; }
        public string Type { get; set; }
        public bool Bold { get; set; }
        [JsonIgnore] public ColumnType ColumnType => ColumnType.Parse(Type);

        public override bool Equals(object obj)
        {
            if (obj is not SourceSchemaData rsd)
                return base.Equals(obj);

            if (Name != rsd.Name)
                return false;

            if (Scope != rsd.Scope)
                return false;

            if (Type != rsd.Type)
                return false;

            if (Bold != rsd.Bold)
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Scope, Type, Bold);
        }
    }

    public class SourceDataColumns : SourceSchemaData
    {
        public Dictionary<int, object> RowValuePairs { get; set; } = new Dictionary<int, object>();
    }

    public class SourceColumns : List<SourceDataColumns>
    {
        public SourceColumns()
        { }

        public SourceColumns(IEnumerable<SourceDataColumns> columns) : base(columns)
        { }

        // Null when the sheet has no column of that kind
        public (SourceColumns Bold, SourceColumns Normal) Split()
        {
            var group = this.GroupBy(x => x.Bold).ToDictionary(x => x.Key);
            var bold = group.TryGetValue(true, out var b) ? new SourceColumns(b) : null;
            var normal = group.TryGetValue(false, out var n) ? new SourceColumns(n) : null;
            return (bold, normal);
        }

        public List<Dictionary<string, object>> Rows()
        {
            var result = new List<Dictionary<string, object>>();
            foreach (var row in this.SelectMany(x => x.RowValuePairs.Keys).Distinct().OrderBy(x => x))
                result.Add(this.ToDictionary(x => x.Name, x => x.RowValuePairs.GetValueOrDefault(row)));

            return result;
        }
    }

    public class SourceSheetData : IExcelFileTrackable
    {
        private string _root, _sheetName, _tableName, _fileName;
        public string Based { get; set; }
        public string Json { get; set; }
        public SourceColumns Columns { get; set; } = new SourceColumns();

        public string Root
        {
            get => Parent?.FullName ?? _root;
            set => _root = value;
        }
        public string SheetName
        {
            get => Parent?.SheetName ?? _sheetName;
            set => _sheetName = value;
        }
        public string TableName
        {
            get => Parent?.Name ?? _tableName;
            set => _tableName = value;
        }
        public string FileName
        {
            get => Parent?.FileName ?? _fileName;
            set => _fileName = value;
        }
        [JsonIgnore] public Sheet Parent { get; set; }
        [JsonIgnore] public List<SourceSchemaData> Schema => Columns.Cast<SourceSchemaData>().OrderBy(x => x.Name).ToList();

        public IEnumerable<SourceDataColumns> GetRowValues(int minRow, int maxRow)
        {
            return Columns.Select(column => new SourceDataColumns
            {
                Name = column.Name,
                Bold = column.Bold,
                Scope = column.Scope,
                Type = column.Type,
                RowValuePairs = column.RowValuePairs.Where(x => x.Key >= minRow && x.Key <= maxRow).ToDictionary(x => x.Key, x => x.Value)
            });
        }

        private IEnumerable<SourceColumns> StaticChunk(int size)
        {
            var buffer = Columns.Select(column =>
            {
                return column.RowValuePairs.GroupBy(x => x.Key / size).ToDictionary(chunk => chunk.Key, chunk => new SourceDataColumns
                {
                    Name = column.Name,
                    Bold = column.Bold,
                    Scope = column.Scope,
                    Type = column.Type,
                    RowValuePairs = chunk.ToDictionary(x => x.Key, x => x.Value)
                });
            }).ToArray();

            var rows = buffer.Max(x => x.Count);
            for (int row = 0; row < rows; row++)
            {
                yield return new SourceColumns(buffer.Select((chunkedColumns, col) =>
                {
                    return chunkedColumns.GetValueOrDefault(row) ?? new SourceDataColumns
                    {
                        Name = Columns[col].Name,
                        Bold = Columns[col].Bold,
                        Scope = Columns[col].Scope,
                        Type = Columns[col].Type,
                        RowValuePairs = new Dictionary<int, object>()
                    };
                }));
            }
        }

        public IEnumerable<SourceColumns> Chunk(int size)
        {
            if (Columns.Exists(x => x.Bold))
            {
                var boldRows = GetBoldColumnRows();
                var chunkedBoldRows = boldRows.Select((x, i) => new { Row = x, Index = x / size }).GroupBy(x => x.Index).Select(x => x.Min(x => x.Row)).ToList();

                for (int i = 0; i < chunkedBoldRows.Count; i++)
                {
                    var begin = chunkedBoldRows[i];
                    var end = i + 1 < chunkedBoldRows.Count ? chunkedBoldRows[i + 1] : int.MaxValue;

                    yield return new SourceColumns(GetRowValues(begin, end - 1));
                }
            }
            else
            {
                foreach (var chunk in StaticChunk(size))
                    yield return chunk;
            }
        }

        public IEnumerable<int> GetBoldColumnRows()
        {
            return Columns.Where(x => x.Bold).SelectMany(x => x.RowValuePairs.Keys).Distinct();
        }

        public int GetMaxRows()
        {
            return Columns.SelectMany(x => x.RowValuePairs.Keys).Distinct().Max();
        }
    }
}
