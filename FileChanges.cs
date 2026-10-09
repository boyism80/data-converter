using ExcelTableConverter.Model;
using Force.Crc32;
using Newtonsoft.Json;

namespace ExcelTableConverter
{
    public class FileChanges
    {
        private const string DSL_FILE_KEY = "dsl.json";

        public List<string> ConstFiles { get; } = new();
        public List<string> EnumFiles { get; } = new();
        public List<string> DataFiles { get; } = new();
        public List<string> ProcessFiles { get; private set; }
        public List<string> UpdatedFiles { get; private set; }
        public List<string> ErrorFiles { get; private set; }
        public List<string> DeletedFiles { get; private set; }
        public SourceSet DeletedSource { get; } = new();
        public Context Loaded { get; private set; }
        public bool DslChanged { get; private set; }
        public bool FullRun { get; private set; }

        public static FileChanges Detect(Context cached, bool fullRun)
        {
            var config = cached.Configuration;
            var changes = new FileChanges { Loaded = new Context(config), FullRun = fullRun };
            var paths = Directory.GetFiles(config.InputDirectory, "*.xlsx", SearchOption.TopDirectoryOnly);

            foreach (var path in paths)
            {
                var fileName = Path.GetFileName(path);
                if (fileName.StartsWith("~$"))
                    continue;

                string crc;
                try
                {
                    crc = Crc(File.ReadAllBytes(path));
                }
                catch (IOException e)
                {
                    throw new IOException($"Cannot open file {fileName}", e);
                }

                if (fullRun == false && cached.Source.CRC.TryGetValue(fileName, out var oldCrc) && oldCrc == crc)
                    continue;

                if (fileName.StartsWith(config.ConstFilePrefix))
                    changes.ConstFiles.Add(path);
                else if (fileName.StartsWith(config.EnumFilePrefix))
                    changes.EnumFiles.Add(path);
                else
                    changes.DataFiles.Add(path);

                changes.Loaded.Source.CRC.Add(fileName, crc);
            }

            var existingFileNames = paths.Select(Path.GetFileName).ToHashSet();
            changes.DeletedFiles = cached.Source.CRC.Keys.Except(existingFileNames).Where(x => x != DSL_FILE_KEY).ToList();
            foreach (var deletedFile in changes.DeletedFiles)
            {
                if (cached.Source.Data.TryGetValue(deletedFile, out var data))
                    changes.DeletedSource.Data.Add(deletedFile, data);
                if (cached.Source.Enum.TryGetValue(deletedFile, out var enums))
                    changes.DeletedSource.Enum.Add(deletedFile, enums);
                if (cached.Source.Const.TryGetValue(deletedFile, out var consts))
                    changes.DeletedSource.Const.Add(deletedFile, consts);
                if (cached.Source.CRC.TryGetValue(deletedFile, out var crc))
                    changes.DeletedSource.CRC.Add(deletedFile, crc);

                cached.Source.Remove(deletedFile);
            }

            changes.UpdatedFiles = changes.Loaded.Source.CRC.Keys.ToList();
            foreach (var updatedFile in changes.UpdatedFiles)
                cached.Source.Remove(updatedFile);

            var errorFiles = LoadErrorFiles();
            foreach (var fileName in changes.DeletedFiles.Concat(changes.UpdatedFiles).Concat(errorFiles))
            {
                try
                {
                    File.Delete(Context.GetCacheFilePath(fileName));
                }
                catch (Exception e)
                {
                    Logger.Error($"Failed to cleanup cache file for {fileName}: {e.Message}");
                }
            }

            changes.ProcessFiles = (fullRun ? changes.UpdatedFiles : changes.UpdatedFiles.Concat(errorFiles)).Distinct().ToList();
            changes.ErrorFiles = fullRun ? new List<string>() : errorFiles;

            var dslCrc = Crc(File.ReadAllBytes(config.DslFilePath));
            changes.DslChanged = fullRun || cached.Source.CRC.TryGetValue(DSL_FILE_KEY, out var oldDslCrc) == false || oldDslCrc != dslCrc;
            changes.Loaded.Source.CRC.Add(DSL_FILE_KEY, dslCrc);
            cached.Source.CRC.Remove(DSL_FILE_KEY);

            return changes;
        }

        private static string Crc(byte[] bytes)
        {
            return $"{Crc32Algorithm.Compute(bytes)}.{bytes.Length}";
        }

        private static List<string> LoadErrorFiles()
        {
            try
            {
                if (File.Exists(Context.ERROR_CACHE_PATH) == false)
                    return new List<string>();

                return JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(Context.ERROR_CACHE_PATH)) ?? new List<string>();
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load error files: {e.Message}");
                return new List<string>();
            }
        }

        public static void SaveErrorFiles(IEnumerable<string> errorFiles)
        {
            try
            {
                File.WriteAllText(Context.ERROR_CACHE_PATH, JsonConvert.SerializeObject(errorFiles.ToList()));
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to save error files: {e.Message}");
            }
        }
    }
}
