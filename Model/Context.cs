using ExcelTableConverter.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Reflection;

namespace ExcelTableConverter.Model
{
    public class Context
    {
        // Bump CACHE_FORMAT when the layout of cache/*.dat changes; a different build version discards every cache.
        private const string CACHE_FORMAT = "2";
        public static readonly string BUILD_VERSION = $"{Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion}/{CACHE_FORMAT}";
        public const string CACHE_DIRECTORY = "cache";
        public static readonly string SOURCE_CACHE_PATH = GetCacheFilePath("raw");
        public static readonly string ERROR_CACHE_PATH = GetCacheFilePath("err");
        public static readonly JsonSerializerSettings CACHE_JSON = new() { TypeNameHandling = TypeNameHandling.Auto };

        private readonly ConcurrentDictionary<string, DataType> _types = new();
        private readonly ConcurrentDictionary<(string Type, object Raw), DataValue> _values = new();

        public AppConfiguration Configuration { get; }
        public Namespace RootNamespace { get; }
        public Namespace EnumNamespace { get; }
        public Namespace ConstNamespace { get; }
        public string Output { get; } = "output";
        public JObject DSL { get; }
        public SourceSet Source { get; private set; } = new();
        public CompletedSet Completed { get; }
        public string BuildVersion { get; private set; } = BUILD_VERSION;

        static Context()
        {
            Directory.CreateDirectory(CACHE_DIRECTORY);
        }

        public Context(AppConfiguration configuration)
        {
            Configuration = configuration;
            RootNamespace = new Namespace(configuration.Namespace);
            EnumNamespace = new Namespace(configuration.EnumNamespace, RootNamespace);
            ConstNamespace = new Namespace(configuration.ConstNamespace, RootNamespace);
            DSL = JObject.Parse(File.ReadAllText(configuration.DslFilePath));
            Completed = new CompletedSet(this);
        }

        public Context Merge(SourceSet source)
        {
            return new Context(Configuration) { Source = Source.Merge(source) };
        }

        public DataType Type(string type, IExcelFileTrackable tracker = null)
        {
            if (_types.TryGetValue(type, out var parsed))
                return parsed;

            return _types.GetOrAdd(type, DataType.Parse(this, type, tracker));
        }

        public DataValue Cast(string type, object raw, IExcelFileTrackable tracker = null)
        {
            return Type(type, tracker).Cast(this, raw, tracker);
        }

        public DataValue CachedValue(string type, object raw, Func<DataValue> cast)
        {
            if (_values.TryGetValue((type, raw), out var value))
                return value;

            return _values.GetOrAdd((type, raw), cast());
        }

        public void Arrange()
        {
            Completed.Build(Source, Configuration, DSL);
        }

        public void Save()
        {
            using var fs = new FileStream(SOURCE_CACHE_PATH, FileMode.Create);
            using var gs = new GZipStream(fs, CompressionMode.Compress);
            using var writer = new BinaryWriter(gs);
            writer.Write(BuildVersion);

            var sourceBytes = Source.ToBytes();
            writer.Write(sourceBytes.Length);
            writer.Write(sourceBytes);
        }

        public bool Load()
        {
            try
            {
                using var fs = new FileStream(SOURCE_CACHE_PATH, FileMode.Open);
                using var gs = new GZipStream(fs, CompressionMode.Decompress);
                using var reader = new BinaryReader(gs);
                BuildVersion = reader.ReadString();
                Source.FromBytes(reader.ReadBytes(reader.ReadInt32()));
                return true;
            }
            catch (Exception)
            {
                Source = new SourceSet();
                return false;
            }
        }

        public static string GetCacheFilePath(string fileName)
        {
            return Path.Combine(CACHE_DIRECTORY, $"{fileName}.dat");
        }
    }
}
