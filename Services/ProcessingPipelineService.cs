#nullable enable
using ExcelTableConverter.Configuration;
using ExcelTableConverter.Model;
using ExcelTableConverter.Worker;
using ExcelTableConverter.Worker.Cache;
using ExcelTableConverter.Worker.Generator;
using ExcelTableConverter.Worker.Loader;
using ExcelTableConverter.Worker.Validator;
using Force.Crc32;
using Newtonsoft.Json;

namespace ExcelTableConverter.Services
{
    /// <summary>
    /// Implementation of processing pipeline service for Excel table conversion
    /// 
    /// Coordinates the sequential execution of data loading, validation, and code generation
    /// stages with proper error handling and progress tracking.
    /// </summary>
    public class ProcessingPipelineService : IProcessingPipelineService
    {
        private readonly AppConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the ProcessingPipelineService
        /// </summary>
        /// <param name="configuration">The application configuration</param>
        public ProcessingPipelineService(AppConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }
        /// <summary>
        /// Executes the complete data processing pipeline
        /// </summary>
        /// <param name="fileResult">The result of file processing containing categorized files</param>
        /// <param name="cachedContext">The cached context from previous runs</param>
        /// <param name="dslFilePath">The path to the DSL configuration file</param>
        /// <returns>The processed context ready for code generation</returns>
        /// <exception cref="InvalidOperationException">Thrown when pipeline execution fails</exception>
        public Task<Context> ExecuteDataProcessingPipelineAsync(FileProcessingResult fileResult, Context cachedContext, string dslFilePath)
        {
            var loaded = fileResult.LoadedContext;

            // Schedule constant file processing
            Scheduler.Add(() =>
            {
                var constWorkBooks = new ExcelFileLoader(loaded, fileResult.ConstFiles.ToList(), quiet: true).Run();
                var constSheets = new SheetLoader(loaded, constWorkBooks, quiet: true).Run();
                new SourceConstLoader(loaded, constSheets).Run();
            });

            // Schedule enum file processing
            Scheduler.Add(() =>
            {
                var enumWorkBooks = new ExcelFileLoader(loaded, fileResult.EnumFiles.ToList(), quiet: true).Run();
                var enumSheets = new SheetLoader(loaded, enumWorkBooks, quiet: true).Run();
                new SourceEnumLoader(loaded, enumSheets).Run();
            });

            // Schedule data file loading
            IReadOnlyList<Workbook>? dataWorkBooks = null;
            Scheduler.Add(() =>
            {
                dataWorkBooks = new ExcelFileLoader(loaded, fileResult.DataFiles.ToList()).Run();
            });

            // Schedule data sheet processing
            IReadOnlyList<Sheet>? dataSheets = null;
            Scheduler.Add(() =>
            {
                if (dataWorkBooks != null)
                {
                    dataSheets = new SheetLoader(loaded, dataWorkBooks).Run();
                }
            });

            // Process data files and merge contexts
            var context = new Context(cachedContext.Configuration);

            Scheduler.Add(() =>
            {
                if (dataSheets != null)
                {
                    new SourceDataLoader(loaded, dataSheets).Run();
                }
                context = cachedContext + loaded;
            });

            // Arrange data and prepare for validation
            Scheduler.Add(() =>
            {
                context.Arrange();
            }, stopOnError: true);

            // Execute scheduled data processing tasks
            Scheduler.Run();

            if (Scheduler.Suspended)
            {
                throw new InvalidOperationException("Data processing pipeline failed");
            }

            return Task.FromResult(context);
        }

        /// <summary>
        /// Executes the validation pipeline
        /// </summary>
        /// <param name="context">The context to validate</param>
        /// <param name="fileResult">The result of file processing containing categorized files</param>
        /// <returns>True if validation passes, false otherwise</returns>
        public bool ExecuteValidationPipeline(Context context, FileProcessingResult fileResult)
        {
            var processFiles = new List<string>(fileResult.ProcessFiles);
            var additionalFiles = GetAdditionalFilesForValidation(context, processFiles, fileResult.DslFileChanged, fileResult);

            // Check for deleted file dependencies and warn user
            if (fileResult.DeletedFiles.Any())
            {
                var deletedFilesDependencies = CheckDeletedFilesDependencies(context, fileResult);
                if (deletedFilesDependencies.Any())
                {
                    foreach (var (deletedFile, referencingFiles) in deletedFilesDependencies)
                    {
                        foreach (var referencingFile in referencingFiles)
                        {
                            additionalFiles.Add(referencingFile);
                        }
                    }
                    Logger.NewLine();
                }
            }


            if (additionalFiles.Any())
            {
                Logger.Comment("변경 또는 삭제된 파일을 참조하는 파일들이 발견되었습니다.", ConsoleColor.DarkGray);
                foreach (var file in additionalFiles)
                {
                    Logger.Comment($"  참조하는 파일 : {file}", ConsoleColor.DarkGray);
                }
                Logger.NewLine();
                processFiles = processFiles.Concat(additionalFiles).ToList();
            }

            // Schedule validation tasks
            Scheduler.Add(() => new NameValidator(context, processFiles).Run());
            Scheduler.Add(() => new SchemaValidator(context).Run());
            Scheduler.Add(() => new KeyValidator(context).Run());
            Scheduler.Add(() => new EnumValidator(context).Run());
            Scheduler.Add(() => new DslValidator(context).Run());
            Scheduler.Add(() => new RelationTypeValidator(context).Run());

            // Schedule relation value validation
            var rvds = new List<RelationValueValidationData>();
            Scheduler.Add(() => rvds.AddRange(new RelationValueTraveller(context, processFiles).Run().SelectMany(x => x)));
            Scheduler.Add(() => new RelationValueValidator(context, rvds).Run());
            Scheduler.Add(() => new StrongTypeValidator(context, processFiles).Run());

            // Execute all scheduled validation tasks
            Scheduler.Run();

            var isComplete = !Scheduler.Suspended;

            Logger.NewLine();
            Logger.NewLine();

            if (!isComplete)
            {
                Logger.WriteLine("테이블 변환 과정에서 에러가 발생했습니다.", foreground: ConsoleColor.Red, decorate: false);
            }
            else
            {
                Logger.WriteLine("테이블 변환과 검증을 완료했습니다.", foreground: ConsoleColor.Blue, decorate: false);
            }

            // Reset logger and scheduler for next phase
            Logger.Reset();
            Scheduler.Reset();

            return isComplete;
        }

        /// <summary>
        /// Executes the code generation pipeline
        /// </summary>
        /// <param name="context">The validated context</param>
        /// <param name="targetLanguages">The target programming languages for code generation</param>
        /// <returns>True if code generation succeeds, false otherwise</returns>
        public Task<bool> ExecuteCodeGenerationPipelineAsync(Context context, IReadOnlySet<string> targetLanguages)
        {
            // Generate JSON files
            Scheduler.Add(() => new JsonFileGenerator(context).Run());

            if (targetLanguages.Contains("go"))
            {
                Scheduler.Add(() => new HasAJsonFileGenerator(context).Run());
            }

            Scheduler.Add(() => new DiffFileGenerator(context).Run());

            // Generate code for each specified language
            foreach (var language in targetLanguages)
            {
                switch (language)
                {
                    case "c++":
                        Scheduler.Add(() => new ExcelTableConverter.Worker.Generator.CPP.ClassFileGenerator(context).Run());
                        break;

                    case "c#":
                        Scheduler.Add(() => new ExcelTableConverter.Worker.Generator.CS.ClassFileGenerator(context).Run());
                        break;

                    case "node":
                        Scheduler.Add(() => new ExcelTableConverter.Worker.Generator.Node.ClassFileGenerator(context).Run());
                        break;

                    case "go":
                        Scheduler.Add(() => new ExcelTableConverter.Worker.Generator.Go.ClassFileGenerator(context).Run());
                        break;
                }
            }

            // Generate CRC files for integrity checking
            Scheduler.Add(() => GenerateCrcFiles(context));

            // Execute all scheduled generation tasks
            Scheduler.Run();

            return Task.FromResult(!Scheduler.Suspended);
        }

        /// <summary>
        /// Updates data cache for the processed files
        /// </summary>
        /// <param name="context">The processed context</param>
        /// <param name="updatedFiles">The list of files that were updated</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public Task UpdateDataCacheAsync(Context context, IReadOnlyList<string> updatedFiles)
        {
            Scheduler.Add(() => new DataCacheWorker(context, updatedFiles.ToList()).Run());
            Scheduler.Run();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Displays processing information and progress
        /// </summary>
        /// <param name="processFiles">The list of files being processed</param>
        /// <param name="updatedFiles">The list of updated files</param>
        /// <param name="errorFiles">The list of files with errors</param>
        public void DisplayProcessingInfo(IReadOnlyList<string> processFiles, IReadOnlyList<string> updatedFiles, IReadOnlyList<string> errorFiles)
        {
            if (processFiles.Any())
            {
                Logger.WriteLine(" 변경된 파일 또는 가장 마지막 에러 발생 파일에 대해서만 작업을 진행합니다.",
                    foreground: ConsoleColor.Blue, decorate: false);

                foreach (var (files, suffix) in new[] { (updatedFiles, "변경된 파일"), (errorFiles, "에러 파일") })
                {
                    if (!files.Any())
                        continue;

                    var fileName = files[0];
                    var anotherFileCount = files.Count - 1;
                    var message = fileName;
                    if (anotherFileCount > 0)
                        message = $"{fileName} 외 {anotherFileCount}개 파일";
                    Logger.Comment($"{message} ({suffix})", ConsoleColor.DarkGray);
                }
            }
            else
            {
                Logger.WriteLine(" 변경된 파일 또는 가장 마지막 에러 발생 파일이 없습니다.",
                    foreground: ConsoleColor.Blue, decorate: false);
            }
            Logger.NewLine();
        }

        /// <summary>
        /// Generates CRC files for integrity checking
        /// </summary>
        /// <param name="context">The context containing the output configuration</param>
        private void GenerateCrcFiles(Context context)
        {
            foreach (var scope in new[] { Scope.Server, Scope.Client })
            {
                var jsonDir = Path.Combine(context.Output, _configuration.JsonFilePath, $"{scope}".ToLower());
                if (!Directory.Exists(jsonDir))
                    continue;

                var crc32 = Directory.GetFiles(jsonDir, "*.json").ToDictionary(
                    path => Path.GetFileName(path),
                    path =>
                    {
                        var bytes = File.ReadAllBytes(path);
                        return Crc32Algorithm.Compute(bytes);
                    });

                var crcFilePath = Path.Combine(jsonDir, "Crc.txt");
                File.WriteAllText(crcFilePath, JsonConvert.SerializeObject(crc32));
            }
            Logger.Complete("CRC 파일을 생성했습니다.");
        }

        /// <summary>
        /// Gets additional files that need validation due to dependencies on modified or deleted files
        /// </summary>
        /// <param name="context">The processing context</param>
        /// <param name="modifiedFiles">List of files that were modified and need processing</param>
        /// <param name="dslFileChanged">Whether DSL file was changed</param>
        /// <param name="fileResult">The file processing result containing deleted files source controller</param>
        /// <returns>Set of additional files that need validation</returns>
        private HashSet<string> GetAdditionalFilesForValidation(Context context, IEnumerable<string> modifiedFiles, bool dslFileChanged, FileProcessingResult fileResult)
        {
            var additionalFiles = new HashSet<string>();

            foreach (var (fileName, sheets) in context.Source.Data)
            {
                // Skip if file is already in modified list
                if (modifiedFiles.Contains(fileName))
                    continue;

                foreach (var sheet in sheets)
                {
                    foreach (var column in sheet.Columns)
                    {
                        var type = Util.Type.Nake(column.Type);

                        // Check relation type dependencies
                        if (Util.Type.IsRelation(column.Type, out _))
                        {
                            if (Util.Type.SplitReferenceType(type, out var referencedTable, out _))
                            {
                                // Find which file contains the referenced table
                                var referencedFile = context.Source.Data
                                    .FirstOrDefault(x => x.Value.Any(s => s.TableName == referencedTable))
                                    .Key;

                                // If not found in current context, check in deleted files
                                if (string.IsNullOrEmpty(referencedFile) && fileResult.DeletedFilesSource != null)
                                {
                                    foreach (var (deletedFileName, deletedSheets) in fileResult.DeletedFilesSource.Data)
                                    {
                                        if (deletedSheets.Any(s => s.TableName == referencedTable))
                                        {
                                            referencedFile = deletedFileName;
                                            break;
                                        }
                                    }
                                }

                                if (modifiedFiles.Contains(referencedFile))
                                {
                                    additionalFiles.Add(fileName);
                                    break;
                                }
                            }
                        }

                        // Check enum dependencies
                        if (context.Source.Enum.TryGetSourceEnum(type, out var source))
                        {
                            // Check direct enum usage
                            var enumFile = source.FileName;

                            if (modifiedFiles.Contains(enumFile))
                            {
                                additionalFiles.Add(fileName);
                                break;
                            }
                        }
                        else if (fileResult.DeletedFilesSource != null)
                        {
                            // Check deleted enum dependencies
                            foreach (var (deletedFileName, deletedEnums) in fileResult.DeletedFilesSource.Enum.Container)
                            {
                                if (deletedEnums.Any(e => e.Table == type) && modifiedFiles.Contains(deletedFileName))
                                {
                                    additionalFiles.Add(fileName);
                                    break;
                                }
                            }
                        }

                        // Check DSL dependencies
                        if (context.DSL != null && context.DSL[type] != null)
                        {
                            if (dslFileChanged)
                            {
                                additionalFiles.Add(fileName);
                                break;
                            }
                        }
                    }
                }
            }

            return additionalFiles;
        }

        /// <summary>
        /// Checks for dependencies on deleted files and returns which files reference them
        /// </summary>
        /// <param name="context">The current context</param>
        /// <param name="fileResult">The file processing result containing deleted files source controller</param>
        /// <returns>Dictionary mapping deleted files to files that reference them</returns>
        private Dictionary<string, List<string>> CheckDeletedFilesDependencies(Context context, FileProcessingResult fileResult)
        {
            var dependencies = new Dictionary<string, List<string>>();

            if (fileResult.DeletedFilesSource == null)
                return dependencies;

            foreach (var deletedFile in fileResult.DeletedFiles)
            {
                var referencingFiles = new List<string>();

                // Check if any current files reference tables/enums from deleted files
                foreach (var (fileName, sheets) in context.Source.Data)
                {
                    foreach (var sheet in sheets)
                    {
                        foreach (var column in sheet.Columns)
                        {
                            var type = Util.Type.Nake(column.Type);

                            // Check relation dependencies
                            if (Util.Type.IsRelation(column.Type, out _))
                            {
                                if (Util.Type.SplitReferenceType(type, out var referencedTable, out _))
                                {
                                    // Check if referenced table is from deleted file
                                    if (fileResult.DeletedFilesSource.Data.TryGetValue(deletedFile, out var deletedSheets))
                                    {
                                        var hasReferencedTable = deletedSheets.Any(s => s.TableName == referencedTable);
                                        if (hasReferencedTable && !referencingFiles.Contains(fileName))
                                        {
                                            referencingFiles.Add(fileName);
                                        }
                                    }
                                }
                            }

                            // Check enum dependencies
                            if (fileResult.DeletedFilesSource.Enum.Container.TryGetValue(deletedFile, out var deletedEnums))
                            {
                                var hasReferencedEnum = deletedEnums.Any(e => e.Table == type);
                                if (hasReferencedEnum && !referencingFiles.Contains(fileName))
                                {
                                    referencingFiles.Add(fileName);
                                }
                            }
                        }
                    }
                }

                if (referencingFiles.Any())
                {
                    dependencies[deletedFile] = referencingFiles;
                }
            }

            return dependencies;
        }
    }
}