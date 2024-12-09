using ExcelTableConverter.Worker;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using System.Data.Common;

namespace ExcelTableConverter.Model
{
    public class RawValue : IExcelFileTrackable
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

    public class RawSchemaData
    {
        public string Name { get; set; }
        public Scope Scope { get; set; }
        public string Type { get; set; }
        public bool Bold { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is not RawSchemaData rsd)
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
    }

    public class RawDataColumns : RawSchemaData
    {
        public Dictionary<int, object> RowValuePairs { get; set; } = new Dictionary<int, object>();
    }

    public class RawSheetData : IExcelFileTrackable
    {
        private string _root, _sheetName, _tableName, _fileName;
        public string Based { get; set; }
        public string Json { get; set; }
        public List<RawDataColumns> Columns { get; set; } = new List<RawDataColumns>();

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
        [JsonIgnore] public List<RawSchemaData> Schema => Columns.Cast<RawSchemaData>().OrderBy(x => x.Name).ToList();

        public IEnumerable<RawDataColumns> GetRowValues(int row)
        {
            return Columns.Select(column => new RawDataColumns
            {
                Name = column.Name,
                Bold = column.Bold,
                Scope = column.Scope,
                Type = column.Type,
                RowValuePairs = column.RowValuePairs.Where(x => x.Key == row).ToDictionary(x => x.Key, x => x.Value)
            });
        }

        public IEnumerable<RawDataColumns> GetRowValues(int minRow, int maxRow)
        {
            return Columns.Select(column => new RawDataColumns
            {
                Name = column.Name,
                Bold = column.Bold,
                Scope = column.Scope,
                Type = column.Type,
                RowValuePairs = column.RowValuePairs.Where(x => x.Key >= minRow && x.Key <= maxRow).ToDictionary(x => x.Key, x => x.Value)
            });
        }

        private IEnumerable<List<RawDataColumns>> StaticChunk(int size)
        {
            var buffer = Columns.Select(column =>
            {
                return column.RowValuePairs.GroupBy(x => x.Key / size).ToDictionary(chunk => chunk.Key, chunk => new RawDataColumns
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
                yield return buffer.Select((chunkedColumns, col) => 
                {
                    return chunkedColumns.GetValueOrDefault(row) ?? new RawDataColumns
                    {
                        Name = Columns[col].Name,
                        Bold = Columns[col].Bold,
                        Scope = Columns[col].Scope,
                        Type = Columns[col].Type,
                        RowValuePairs = new Dictionary<int, object>()
                    };
                }).ToList();
            }
        }

        public IEnumerable<List<RawDataColumns>> Chunk(int size)
        {
            if (Columns.Exists(x => x.Bold))
            {
                var boldRows = GetBoldColumnRows();
                var chunkedBoldRows = boldRows.Select((x, i) => new { Row = x, Index = x / size }).GroupBy(x => x.Index).Select(x => x.Min(x => x.Row)).ToList();

                for (int i = 0; i < chunkedBoldRows.Count; i++)
                {
                    var begin = chunkedBoldRows.ElementAtOrDefault(i, int.MinValue);
                    var end = chunkedBoldRows.ElementAtOrDefault(i + 1, int.MaxValue);

                    yield return GetRowValues(begin, end - 1).ToList();
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
