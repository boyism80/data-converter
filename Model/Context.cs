using ExcelTableConverter.Configuration;
using ExcelTableConverter.Controller;
using ExcelTableConverter.Factory;
using ExcelTableConverter.Services;
using ExcelTableConverter.Worker;
using ExcelTableConverter.Worker.Cache;
using ExcelTableConverter.Worker.Generator;
using ExcelTableConverter.Worker.Loader;
using ExcelTableConverter.Worker.Validator;
using Force.Crc32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace ExcelTableConverter.Model
{
    /// <summary>
    /// Central context class for Excel table conversion process
    /// 
    /// Manages the complete state of the conversion process including source data,
    /// completed data, DSL configuration, and caching mechanisms.
    /// </summary>
    public class Context
    {
        public static string BUILD_VERSION = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        public const string CACHE_DIRECTORY = "cache";
        public const string SOURCE_CACHE_FILE = "raw";
        public const string ERROR_FILE = "err";
        public readonly static string SOURCE_CACHE_PATH = GetCacheFilePath(SOURCE_CACHE_FILE);
        public readonly static string ERROR_CACHE_PATH = GetCacheFilePath(ERROR_FILE);

        private readonly CastValueFactory _castFactory;

        [JsonIgnore]
        private AppConfiguration _configuration;

        /// <summary>
        /// Gets the application configuration instance
        /// </summary>
        [JsonIgnore]
        public AppConfiguration Configuration => _configuration;

        [JsonIgnore]
        public string Output = "output";

        [JsonIgnore]
        public JObject DSL { get; private set; }

        public SourceController Source { get; private set; }
        public string BuildVersion { get; set; } = BUILD_VERSION;

        [JsonIgnore] public CompletedController Completed { get; set; }

        static Context()
        {
            if (Directory.Exists(CACHE_DIRECTORY) == false)
                Directory.CreateDirectory(CACHE_DIRECTORY);
        }

        public Context()
        {
            Source = new SourceController(this);
            Completed = new CompletedController(this);
            _castFactory = new CastValueFactory(this);
        }

        public Context(AppConfiguration configuration) : this()
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            DSL = JObject.Parse(File.ReadAllText(_configuration.DslFilePath));
        }

        public static Context operator +(Context ctx1, Context ctx2)
        {
            var result = new Context(ctx1._configuration);

            // Merge Source controllers
            var mergedSourceEnum = ctx1.Source.Enum.Container.Concat(ctx2.Source.Enum.Container)
                .ToDictionary(x => x.Key, x => x.Value);
            var mergedSourceData = ctx1.Source.Data.Container.Concat(ctx2.Source.Data.Container)
                .ToDictionary(x => x.Key, x => x.Value);
            var mergedSourceConst = ctx1.Source.Const.Container.Concat(ctx2.Source.Const.Container)
                .ToDictionary(x => x.Key, x => x.Value);
            var mergedCRC = ctx1.Source.CRC.Concat(ctx2.Source.CRC)
                .ToDictionary(x => x.Key, x => x.Value);

            result.Source = new SourceController(result, mergedSourceConst, mergedSourceData, mergedSourceEnum, mergedCRC);

            return result;
        }

        public void SetConfiguration(AppConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public object Cast(string type, object value)
        {
            return _castFactory.Build(type, value);
        }

        public bool Save()
        {
            try
            {
                using var fs = new FileStream(GetCacheFilePath(SOURCE_CACHE_FILE), FileMode.Create);
                using var gs = new GZipStream(fs, CompressionMode.Compress);
                using var writer = new BinaryWriter(gs);
                writer.Write(BuildVersion);

                var sourceBytes = Source.ToBytes();
                writer.Write(sourceBytes.Length);
                writer.Write(sourceBytes);

                var completedBytes = Completed.ToBytes();
                writer.Write(completedBytes.Length);
                writer.Write(completedBytes);

                var dslBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(DSL));
                writer.Write(dslBytes.Length);
                writer.Write(dslBytes);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public bool Load()
        {
            try
            {
                using var fs = new FileStream(GetCacheFilePath(SOURCE_CACHE_FILE), FileMode.Open);
                using var gs = new GZipStream(fs, CompressionMode.Decompress);
                using var reader = new BinaryReader(gs);

                BuildVersion = reader.ReadString();

                // Read Source
                var sourceLength = reader.ReadInt32();
                var sourceBytes = reader.ReadBytes(sourceLength);
                Source.FromBytes(sourceBytes);

                // Read Completed
                var completedLength = reader.ReadInt32();
                var completedBytes = reader.ReadBytes(completedLength);
                Completed.FromBytes(completedBytes);

                // Read DSL
                var dslLength = reader.ReadInt32();
                var dslBytes = reader.ReadBytes(dslLength);
                DSL = JObject.Parse(Encoding.UTF8.GetString(dslBytes));

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Arrange()
        {
            // Step 1: Build Enum data first (needed for DataTypeCaster)
            Completed.Enum.BuildFromSourceData(Source.Enum, _configuration.DslTypeEnumName, DSL);

            // Step 2: Build Schema data (needed for DataTypeCaster)
            Completed.Schema.BuildFromSourceData(Source.Data, _configuration);

            // Step 3: Build Data (requires Schema and Enum to be ready)
            var dataConvertResults = new DataTypeCaster(this).Run();
            Completed.Data.BuildFromDataTypeCaster(dataConvertResults);

            // Step 4: Build Const data
            Completed.Const.BuildFromSourceData(Source.Const, Cast);
        }

        public static string GetCacheFilePath(string fileName)
        {
            return Path.Combine(CACHE_DIRECTORY, $"{fileName}.dat");
        }
    }
}