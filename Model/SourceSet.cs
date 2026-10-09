using Newtonsoft.Json;
using System.Text;

namespace ExcelTableConverter.Model
{
    public class SourceConstMap : Dictionary<string, List<SourceConst>>
    { }

    public class SourceEnumMap : Dictionary<string, List<SourceEnum>>
    {
        public bool TryGetSourceEnum(string type, out SourceEnum sourceEnum)
        {
            sourceEnum = Values.SelectMany(x => x).FirstOrDefault(x => x.SheetName == type);
            return sourceEnum != null;
        }
    }

    public class SourceDataMap : Dictionary<string, List<SourceSheetData>>
    {
        public HashSet<string> TableNames => Values.SelectMany(x => x).Select(x => x.TableName).ToHashSet();

        public List<SourceDataColumns> FindColumns(string tableName)
        {
            return Values.SelectMany(x => x).FirstOrDefault(x => x.TableName == tableName)?.Columns;
        }

        public SourceSheetData FindSheet(SourceDataColumns column)
        {
            return Values.SelectMany(x => x).FirstOrDefault(x => x.Columns.Contains(column));
        }
    }

    public class SourceSet
    {
        public SourceConstMap Const { get; private set; } = new();
        public SourceDataMap Data { get; private set; } = new();
        public SourceEnumMap Enum { get; private set; } = new();
        public Dictionary<string, string> CRC { get; private set; } = new();

        public void Remove(string fileName)
        {
            Const.Remove(fileName);
            Enum.Remove(fileName);
            Data.Remove(fileName);
            CRC.Remove(fileName);
        }

        public SourceSet Merge(SourceSet other)
        {
            var result = new SourceSet();
            foreach (var (k, v) in Const.Concat(other.Const).OrderBy(x => x.Key, StringComparer.Ordinal))
                result.Const.Add(k, v);
            foreach (var (k, v) in Data.Concat(other.Data).OrderBy(x => x.Key, StringComparer.Ordinal))
                result.Data.Add(k, v.OrderBy(x => x.SheetName, StringComparer.Ordinal).ToList());
            foreach (var (k, v) in Enum.Concat(other.Enum).OrderBy(x => x.Key, StringComparer.Ordinal))
                result.Enum.Add(k, v);
            foreach (var (k, v) in CRC.Concat(other.CRC).OrderBy(x => x.Key, StringComparer.Ordinal))
                result.CRC.Add(k, v);
            return result;
        }

        public byte[] ToBytes()
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            foreach (var section in new object[] { Const, Data, Enum, CRC })
            {
                var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(section));
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
            return ms.ToArray();
        }

        public void FromBytes(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);
            using var reader = new BinaryReader(ms);
            Const = Read<SourceConstMap>(reader);
            Data = Read<SourceDataMap>(reader);
            Enum = Read<SourceEnumMap>(reader);
            CRC = Read<Dictionary<string, string>>(reader);
        }

        private static T Read<T>(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(reader.ReadInt32());
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes));
        }
    }
}
