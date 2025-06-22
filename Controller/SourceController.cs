using ExcelTableConverter.Model;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Text;

namespace ExcelTableConverter.Controller
{
    using SourceConstContainer = Dictionary<string, List<SourceConst>>;
    using SourceDataContainer = Dictionary<string, List<SourceSheetData>>;
    using SourceEnumContainer = Dictionary<string, List<SourceEnum>>;

    /// <summary>
    /// Manages all source data containers and provides unified access to source data operations
    /// Coordinates between SourceConstController, SourceDataController, and SourceEnumController
    /// </summary>
    public class SourceController
    {
        private readonly ConcurrentDictionary<object, object> _cache = new ConcurrentDictionary<object, object>();
        private readonly Context _context;

        /// <summary>
        /// Gets the source constant data controller
        /// </summary>
        public SourceConstController Const { get; }

        /// <summary>
        /// Gets the source data controller
        /// </summary>
        public SourceDataController Data { get; }

        /// <summary>
        /// Gets the source enum data controller
        /// </summary>
        public SourceEnumController Enum { get; }

        /// <summary>
        /// Gets the CRC values for change detection
        /// </summary>
        public Dictionary<string, string> CRC { get; private set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets all table names from source data
        /// </summary>
        public HashSet<string> AllTableNames => Data.GetAllTableNames();

        /// <summary>
        /// Initializes a new instance of the SourceController class
        /// </summary>
        /// <param name="context">Context instance for configuration and data access</param>
        public SourceController(Context context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            Const = new SourceConstController(context);
            Data = new SourceDataController(context);
            Enum = new SourceEnumController(context);
        }

        /// <summary>
        /// Initializes a new instance of the SourceController class with existing containers
        /// </summary>
        /// <param name="context">Context instance for configuration and data access</param>
        /// <param name="constContainer">Source constant container</param>
        /// <param name="dataContainer">Source data container</param>
        /// <param name="enumContainer">Source enum container</param>
        /// <param name="crc">CRC dictionary for change detection</param>
        public SourceController(
            Context context,
            SourceConstContainer constContainer,
            SourceDataContainer dataContainer,
            SourceEnumContainer enumContainer,
            Dictionary<string, string> crc)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            Const = new SourceConstController(context, constContainer);
            Data = new SourceDataController(context, dataContainer);
            Enum = new SourceEnumController(context, enumContainer);
            CRC = crc ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Validates all source data for consistency and integrity
        /// </summary>
        /// <returns>True if all validations pass</returns>
        public bool ValidateAll()
        {
            var constValid = Const.ValidateAll();
            var dataValid = Data.ValidateAll();
            var enumValid = Enum.ValidateAll();

            return constValid && dataValid && enumValid;
        }

        /// <summary>
        /// Gets overall statistics about source data
        /// </summary>
        /// <returns>Source data statistics</returns>
        public SourceStatistics GetStatistics()
        {
            return new SourceStatistics
            {
                ConstTableCount = Const.GetTableCount(),
                DataTableCount = Data.GetTableCount(),
                EnumTableCount = Enum.GetTableCount(),
                TotalCrcEntries = CRC.Count
            };
        }

        /// <summary>
        /// Validates cross-references between different source data types
        /// </summary>
        /// <returns>True if all cross-references are valid</returns>
        public bool ValidateCrossReferences()
        {
            // TODO: Implement cross-reference validation logic
            // For example, validate that enum references in data exist in enum container
            return true;
        }

        /// <summary>
        /// Clears all cached values
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
            Const.ClearCache();
            Data.ClearCache();
            Enum.ClearCache();
        }

        /// <summary>
        /// Merges another SourceController into this one
        /// </summary>
        /// <param name="other">Other SourceController to merge</param>
        /// <returns>New SourceController with merged data</returns>
        public SourceController Merge(SourceController other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            var mergedConst = Const.Container.Concat(other.Const.Container).ToDictionary(x => x.Key, x => x.Value);
            var mergedData = Data.Container.Concat(other.Data.Container).ToDictionary(x => x.Key, x => x.Value);
            var mergedEnum = Enum.Container.Concat(other.Enum.Container).ToDictionary(x => x.Key, x => x.Value);
            var mergedCrc = CRC.Concat(other.CRC).ToDictionary(x => x.Key, x => x.Value);

            return new SourceController(_context, mergedConst, mergedData, mergedEnum, mergedCrc);
        }

        public byte[] ToBytes()
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            var constBytes = Const.ToBytes();
            var dataBytes = Data.ToBytes();
            var enumBytes = Enum.ToBytes();

            writer.Write(constBytes.Length);
            writer.Write(constBytes);
            writer.Write(dataBytes.Length);
            writer.Write(dataBytes);
            writer.Write(enumBytes.Length);
            writer.Write(enumBytes);

            var crcBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(CRC));
            writer.Write(crcBytes.Length);
            writer.Write(crcBytes);

            return ms.ToArray();
        }

        public bool FromBytes(byte[] bytes)
        {
            try
            {
                using var ms = new MemoryStream(bytes);
                using var reader = new BinaryReader(ms);

                var constBytes = reader.ReadBytes(reader.ReadInt32());
                var dataBytes = reader.ReadBytes(reader.ReadInt32());
                var enumBytes = reader.ReadBytes(reader.ReadInt32());

                Const.FromBytes(constBytes);
                Data.FromBytes(dataBytes);
                Enum.FromBytes(enumBytes);

                var crcLength = reader.ReadInt32();
                var crcBytes = reader.ReadBytes(crcLength);
                CRC = JsonConvert.DeserializeObject<Dictionary<string, string>>(Encoding.UTF8.GetString(crcBytes));

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
            Const.Clear();
            Data.Clear();
            Enum.Clear();
            CRC.Clear();
        }
    }

    /// <summary>
    /// Statistics information about source data
    /// </summary>
    public class SourceStatistics
    {
        /// <summary>
        /// Gets or sets the number of constant tables
        /// </summary>
        public int ConstTableCount { get; set; }

        /// <summary>
        /// Gets or sets the number of data tables
        /// </summary>
        public int DataTableCount { get; set; }

        /// <summary>
        /// Gets or sets the number of enum tables
        /// </summary>
        public int EnumTableCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of CRC entries
        /// </summary>
        public int TotalCrcEntries { get; set; }

        /// <summary>
        /// Gets the total number of tables across all types
        /// </summary>
        public int TotalTableCount => ConstTableCount + DataTableCount + EnumTableCount;
    }
}