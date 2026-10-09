using ExcelTableConverter.Configuration;
using ExcelTableConverter.Model;
using ExcelTableConverter.Worker;
using ExcelTableConverter.Worker.Cache;
using ExcelTableConverter.Worker.Generator;
using ExcelTableConverter.Worker.Loader;
using ExcelTableConverter.Worker.Validator;
using Force.Crc32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ExcelTableConverter
{
    public class Converter
    {
        private readonly AppConfiguration _config;

        public Converter(AppConfiguration config)
        {
            _config = config;
        }

        public bool Run()
        {
            try
            {
                var cached = new Context(_config);
                var fullRun = cached.Load() && cached.BuildVersion != Context.BUILD_VERSION;
                var changes = FileChanges.Detect(cached, fullRun);
                PrintChanges(changes);

                var (context, dependents) = Load(cached, changes);
                context.Save();

                if (Validate(context, changes, dependents) == false || Generate(context) == false)
                {
                    FileChanges.SaveErrorFiles(Logger.ErrorFiles);
                    return false;
                }

                if (changes.ProcessFiles.Count > 0 || dependents.Count > 0)
                    new Pipeline().Step(() => new DataCacheWorker(context).Run()).Run();

                FileChanges.SaveErrorFiles(new List<string>());
                return true;
            }
            catch (Exception e)
            {
                Logger.Error($"Conversion process failed: {e.Message}");
                FileChanges.SaveErrorFiles(Logger.ErrorFiles);
                return false;
            }
        }

        private static void PrintChanges(FileChanges changes)
        {
            if (changes.FullRun)
            {
                Logger.WriteLine(" 빌드 버전 변경으로 인해 전체 파일을 재처리합니다.", foreground: ConsoleColor.Blue, decorate: false);
            }
            else if (changes.ProcessFiles.Count > 0)
            {
                Logger.WriteLine(" 변경된 파일 또는 가장 마지막 에러 발생 파일에 대해서만 작업을 진행합니다.", foreground: ConsoleColor.Blue, decorate: false);

                var updatedFiles = changes.ProcessFiles.Except(changes.ErrorFiles).ToList();
                foreach (var (files, suffix) in new[] { (updatedFiles, "변경된 파일"), (changes.ErrorFiles, "에러 파일") })
                {
                    if (files.Count == 0)
                        continue;

                    var message = files.Count > 1 ? $"{files[0]} 외 {files.Count - 1}개 파일" : files[0];
                    Logger.Comment($"{message} ({suffix})", ConsoleColor.DarkGray);
                }
            }
            else
            {
                Logger.WriteLine(" 변경된 파일 또는 가장 마지막 에러 발생 파일이 없습니다.", foreground: ConsoleColor.Blue, decorate: false);
            }
            Logger.NewLine();
        }

        private static (Context Context, HashSet<string> Dependents) Load(Context cached, FileChanges changes)
        {
            var loaded = changes.Loaded;
            var context = cached;
            var dependents = new HashSet<string>();

            var succeeded = new Pipeline()
                .Step(() =>
                {
                    var workbooks = new ExcelFileLoader(loaded, changes.ConstFiles, quiet: true).Run();
                    var sheets = new SheetLoader(loaded, workbooks, quiet: true).Run();
                    new SourceConstLoader(loaded, sheets).Run();
                })
                .Step(() =>
                {
                    var workbooks = new ExcelFileLoader(loaded, changes.EnumFiles, quiet: true).Run();
                    var sheets = new SheetLoader(loaded, workbooks, quiet: true).Run();
                    new SourceEnumLoader(loaded, sheets).Run();
                })
                .Step(() =>
                {
                    var workbooks = new ExcelFileLoader(loaded, changes.DataFiles).Run();
                    var sheets = new SheetLoader(loaded, workbooks).Run();
                    new SourceDataLoader(loaded, sheets).Run();
                    context = cached.Merge(loaded.Source);

                    // A dependent's cached cast result holds values read from other files, so it is cast again.
                    dependents = FindDependents(context, changes);
                    foreach (var file in dependents)
                        File.Delete(Context.GetCacheFilePath(file));
                })
                .Step(() => context.Arrange(), stopOnError: true)
                .Run();

            if (succeeded == false)
                throw new InvalidOperationException("Data processing pipeline failed");

            return (context, dependents);
        }

        private bool Validate(Context context, FileChanges changes, HashSet<string> dependents)
        {
            if (dependents.Count > 0)
            {
                Logger.Comment("변경 또는 삭제된 파일을 참조하는 파일들이 발견되었습니다.", ConsoleColor.DarkGray);
                foreach (var file in dependents)
                    Logger.Comment($"  참조하는 파일 : {file}", ConsoleColor.DarkGray);
                Logger.NewLine();
            }

            var processFiles = changes.ProcessFiles.Concat(dependents).ToList();
            var relations = new List<RelationValueValidationData>();
            var succeeded = new Pipeline()
                .Step(() => new NameValidator(context, processFiles).Run())
                .Step(() => new SchemaValidator(context).Run())
                .Step(() => new KeyValidator(context).Run())
                .Step(() => new EnumValidator(context).Run())
                .Step(() => new DslValidator(context).Run())
                .Step(() => new RelationTypeValidator(context).Run())
                .Step(() => relations.AddRange(new RelationValueTraveller(context, processFiles).Run().SelectMany(x => x)))
                .Step(() => new RelationValueValidator(context, relations).Run())
                .Step(() => new StrongTypeValidator(context, processFiles).Run())
                .Run();

            Logger.NewLine();
            Logger.NewLine();
            if (succeeded)
                Logger.WriteLine("테이블 변환과 검증을 완료했습니다.", foreground: ConsoleColor.Blue, decorate: false);
            else
                Logger.WriteLine("테이블 변환 과정에서 에러가 발생했습니다.", foreground: ConsoleColor.Red, decorate: false);
            Logger.Reset();

            return succeeded;
        }

        private bool Generate(Context context)
        {
            var languages = _config.TargetLanguages;
            var pipeline = new Pipeline().Step(() => new JsonFileGenerator(context).Run());
            if (languages.Contains("go"))
                pipeline.Step(() => new GoJsonFileGenerator(context).Run());
            pipeline.Step(() => new DiffFileGenerator(context).Run());

            foreach (var language in languages)
            {
                switch (language)
                {
                    case "c++":
                        pipeline.Step(() => new CppModelGenerator(context).Run());
                        break;

                    case "c#":
                        pipeline.Step(() => new CsModelGenerator(context).Run());
                        break;

                    case "node":
                        pipeline.Step(() => new NodeModelGenerator(context).Run());
                        break;

                    case "go":
                        pipeline.Step(() => new GoModelGenerator(context).Run());
                        break;
                }
            }

            return pipeline.Step(() => WriteCrc(context)).Run();
        }

        private void WriteCrc(Context context)
        {
            foreach (var (_, scopeName) in _config.DefinedScopes)
            {
                var jsonDir = Path.Combine(context.Output, _config.JsonFilePath, scopeName);
                if (Directory.Exists(jsonDir) == false)
                    continue;

                var crc32 = Directory.GetFiles(jsonDir, "*.json").ToDictionary(Path.GetFileName, path => Crc32Algorithm.Compute(File.ReadAllBytes(path)));
                File.WriteAllText(Path.Combine(jsonDir, "Crc.txt"), JsonConvert.SerializeObject(crc32));
            }
            Logger.Complete("CRC 파일을 생성했습니다.");
        }

        // Files to cast and validate again although their own cells did not change: files that read a changed or deleted
        // file, directly or through another dependent. A file reads the tables and enums its column types reach, including
        // array and map elements and the parameter types of every DSL, and the const tables its cells refer to.
        private static HashSet<string> FindDependents(Context context, FileChanges changes)
        {
            var source = context.Source;
            var deleted = changes.DeletedSource;
            var tableFiles = source.Data.Concat(deleted.Data).SelectMany(x => x.Value.Select(sheet => (sheet.TableName, File: x.Key))).ToLookup(x => x.TableName, x => x.File);
            var enumFiles = source.Enum.Concat(deleted.Enum).SelectMany(x => x.Value.Select(e => (e.Table, File: x.Key))).ToLookup(x => x.Table, x => x.File);
            var constFiles = source.Const.Concat(deleted.Const).SelectMany(x => x.Value.Select(c => (c.TableName, File: x.Key))).ToLookup(x => x.TableName, x => x.File);
            var dslTypes = context.DSL.Properties().SelectMany(x => x.Value as JArray ?? new JArray()).Select(x => x["type"]?.Value<string>()).Where(x => x != null).ToList();

            var reads = new Dictionary<string, HashSet<string>>();
            var dslUsers = new HashSet<string>();
            foreach (var (fileName, sheets) in source.Data)
            {
                var files = new HashSet<string>();
                var columns = sheets.SelectMany(x => x.Columns).ToList();
                var types = new Stack<string>(columns.Select(x => x.Type).Distinct());
                var visited = new HashSet<string>();
                while (types.TryPop(out var type))
                {
                    if (visited.Add(type) == false)
                        continue;

                    var columnType = ColumnType.Parse(type);
                    if (columnType.RelationTable != null)
                        files.UnionWith(tableFiles[columnType.RelationTable]);

                    var naked = columnType.Naked;
                    if (DataType.IsArray(naked, out var element))
                    {
                        types.Push(element);
                    }
                    else if (DataType.IsMap(naked, out var key, out var value))
                    {
                        types.Push(key);
                        types.Push(value);
                    }
                    else if (naked == "dsl")
                    {
                        dslUsers.Add(fileName);
                        foreach (var dslType in dslTypes)
                            types.Push(dslType);
                    }
                    else
                    {
                        files.UnionWith(enumFiles[naked]);
                    }
                }

                foreach (var value in columns.SelectMany(x => x.RowValuePairs.Values))
                {
                    if (value is string s && s.StartsWith("Const:"))
                        files.UnionWith(constFiles[s.Split(':')[1]]);
                }

                files.Remove(fileName);
                reads.Add(fileName, files);
            }

            var changed = changes.ProcessFiles.Concat(changes.DeletedFiles).ToHashSet();
            var dependents = new HashSet<string>();
            var added = true;
            while (added)
            {
                added = false;
                foreach (var (fileName, files) in reads)
                {
                    if (changed.Contains(fileName))
                        continue;

                    if (files.Overlaps(changed) || (changes.DslChanged && dslUsers.Contains(fileName)))
                    {
                        changed.Add(fileName);
                        dependents.Add(fileName);
                        added = true;
                    }
                }
            }

            return dependents;
        }
    }
}
