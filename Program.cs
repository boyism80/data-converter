using ExcelTableConverter;
using ExcelTableConverter.Model;
using ExcelTableConverter.Util;
using ExcelTableConverter.Worker;
using ExcelTableConverter.Worker.Cache;
using ExcelTableConverter.Worker.Generator;
using ExcelTableConverter.Worker.Loader;
using ExcelTableConverter.Worker.Validator;
using Force.Crc32;
using NDesk.Options;
using Newtonsoft.Json;

/// <summary>
/// Excel Table Converter - Main program entry point
/// 
/// This application converts Excel files to various programming language formats
/// including C++, C#, Node.js, and Go. It processes Excel files containing
/// game data tables and generates strongly-typed code representations.
/// </summary>
Logger.OnDecorate = Scheduler.ConsoleDecorator;

try
{
    // Initialize command line parameters with default values
    var dir = Path.Combine("..", "..", "..", "..");  // Default input directory
    var languages = "c++";                           // Default target language
    var dsl = "dsl.json";                            // Default DSL configuration file
    var env = string.Empty;                          // Environment variable

    // Configure command line option parser
    var options = new OptionSet
    {
        { "d|dir=", "input directory", v => dir = v },
        { "l|lang=", "code language", v => languages = v },
        { "e|env=", "code language", v => env = v },
        { "dsl=", "dsl file path", v => dsl = v },
    };

    options.Parse(args);

    // Set environment variable for configuration
    Environment.SetEnvironmentVariable("env", env);
    
    // Clear console if running in TTY mode
    if (Logger.TTY)
    {
        Console.Clear();
    }

    // Load or initialize cached context
    Context cached;
    try
    {
        cached = File.Exists(Context.RAW_CACHE_PATH) ? ZipUtil.Unzip<Context>(File.ReadAllBytes(Context.RAW_CACHE_PATH)) : new Context();
    }
    catch (Exception)
    {
        cached = new Context();
    }

    // Check if build version has changed and clear cache if necessary
    if (cached.BuildVersion != Context.BUILD_VERSION)
    {
        cached = new Context();
        if (Directory.Exists(Context.CACHE_DIRECTORY))
        {
            foreach (var file in Directory.GetFiles(Context.CACHE_DIRECTORY, "*", SearchOption.TopDirectoryOnly))
                File.Delete(file);
        }

        Logger.WriteLine(" 컨버터 빌드 버전이 변경되어 캐시파일을 전부 제거했습니다.", foreground: ConsoleColor.Blue, decorate: false);
    }
    
    var loaded = new Context();

    // Categorize Excel files by type (enum, const, data)
    var enumFiles = new List<string>();
    var constFiles = new List<string>();
    var dataFiles = new List<string>();
    var paths = Directory.GetFiles(dir, "*.xlsx", SearchOption.TopDirectoryOnly);
    
    foreach (var p in paths)
    {
        try
        {
            // Skip temporary Excel files (starting with ~$)
            if (Path.GetFileName(p).StartsWith("~$"))
                continue;

            // Calculate CRC32 checksum for change detection
            var bytes = File.ReadAllBytes(p);
            var crc = $"{Crc32Algorithm.Compute(bytes)}.{bytes.Length}";
            var fname = Path.GetFileName(p);
            
            // Skip processing if file hasn't changed (same CRC)
            if (cached.CRC.TryGetValue(fname, out var old) && old == crc)
                continue;

            // Categorize files based on filename prefix
            if (fname.StartsWith(Context.Config.ConstFilePrefix))
            {
                constFiles.Add(p);
            }
            else if (fname.StartsWith(Context.Config.EnumFilePrefix))
            {
                enumFiles.Add(p);
            }
            else
            {
                dataFiles.Add(p);
            }
            loaded.CRC.Add(fname, crc);
        }
        catch (IOException)
        {
            throw new LogicException($"{Path.GetFileName(p)} 파일을 열 수 없습니다.".AsSpan());
        }
        catch (Exception e)
        {
            throw new AggregateException($"{Path.GetFileName(p)} 파일을 여는 과정에서 문제가 발생했습니다.", e);
        }
    }

    // Handle deleted files - remove from cache
    var deletedFiles = cached.CRC.Keys.Except(paths.Select(p => Path.GetFileName(p))).ToList();
    foreach (var deletedFile in deletedFiles)
    {
        if (cached.RawConst.ContainsKey(deletedFile))
        {
            cached.RawConst.Remove(deletedFile);
        }
        else if (cached.RawEnum.ContainsKey(deletedFile))
        {
            cached.RawEnum.Remove(deletedFile);
        }
        else if (cached.RawData.ContainsKey(deletedFile))
        {
            cached.RawData.Remove(deletedFile);
        }

        cached.CRC.Remove(deletedFile);
    }

    // Handle updated files - remove from cache to force reprocessing
    var updatedFiles = loaded.CRC.Keys.ToList();
    foreach (var updatedFile in updatedFiles)
    {
        if (cached.RawConst.ContainsKey(updatedFile))
        {
            cached.RawConst.Remove(updatedFile);
        }
        else if (cached.RawEnum.ContainsKey(updatedFile))
        {
            cached.RawEnum.Remove(updatedFile);
        }
        else if (cached.RawData.ContainsKey(updatedFile))
        {
            cached.RawData.Remove(updatedFile);
        }

        cached.CRC.Remove(updatedFile);
    }

    // Load error files from previous run
    var errorFiles = File.Exists(Context.ERROR_CACHE_PATH) ? JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(Context.ERROR_CACHE_PATH)) : new List<string>();

    // Clean up cache files for deleted, updated, and error files
    foreach (var fileName in deletedFiles.Concat(updatedFiles).Concat(errorFiles))
    {
        var cacheFilePath = Context.GetCacheFilePath(fileName);
        if (File.Exists(cacheFilePath))
        {
            File.Delete(cacheFilePath);
        }
    }

    // Display processing information
    var processFiles = updatedFiles.Concat(errorFiles).ToList();
    if (processFiles.Any())
    {
        Logger.WriteLine(" 변경된 파일 또는 가장 마지막 에러 발생 파일에 대해서만 작업을 진행합니다.", foreground: ConsoleColor.Blue, decorate: false);

        foreach (var (files, suffix) in new[] { (updatedFiles, "변경된 파일"), (errorFiles, "에러 파일") })
        {
            if (files.Any() == false)
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
        Logger.WriteLine(" 변경된 파일 또는 가장 마지막 에러 발생 파일이 없습니다.", foreground: ConsoleColor.Blue, decorate: false);
    }
    Logger.NewLine();

    // Schedule constant file processing
    Scheduler.Add(() =>
    {
        var constWorkBooks = new ExcelFileLoader(loaded, constFiles, quiet: true).Run();
        var constSheets = new SheetLoader(loaded, constWorkBooks, quiet: true).Run();
        new RawConstLoader(loaded, constSheets).Run();
    });

    // Schedule enum file processing
    Scheduler.Add(() =>
    {
        var enumWorkBooks = new ExcelFileLoader(loaded, enumFiles, quiet: true).Run();
        var enumSheets = new SheetLoader(loaded, enumWorkBooks, quiet: true).Run();
        new RawEnumLoader(loaded, enumSheets).Run();
    });

    // Schedule data file loading
    IReadOnlyList<Workbook> dataWorkBooks = null;
    Scheduler.Add(() =>
    {
        dataWorkBooks = new ExcelFileLoader(loaded, dataFiles).Run();
    });

    // Schedule data sheet processing
    IReadOnlyList<Sheet> dataSheets = null;
    Scheduler.Add(() =>
    {
        dataSheets = new SheetLoader(loaded, dataWorkBooks).Run();
    });

    // Process data files and merge contexts
    var ctx = new Context();
    Scheduler.Add(() =>
    {
        new RawDataLoader(loaded, dataSheets).Run();
        ctx = cached + loaded;
        ctx.ReadDslFile(dsl);
        ctx.ReadConfigFile();

        // Update cache file
        if (File.Exists(Context.RAW_CACHE_PATH))
            File.Delete(Context.RAW_CACHE_PATH);

        File.WriteAllBytes(Context.RAW_CACHE_PATH, ZipUtil.Zip(ctx));
    });

    // Arrange data and prepare for validation
    var isCastValues = false;
    Scheduler.Add(() =>
    {
        ctx.Arrange();
        isCastValues = true;
    }, stopOnError: true);

    // Schedule validation tasks
    Scheduler.Add(() => new NameValidator(ctx, processFiles).Run());
    Scheduler.Add(() => new SchemaValidator(ctx).Run());
    Scheduler.Add(() => new KeyValidator(ctx).Run());
    Scheduler.Add(() => new EnumValidator(ctx).Run());
    Scheduler.Add(() => new DslValidator(ctx).Run());
    Scheduler.Add(() => new RelationTypeValidator(ctx).Run());

    // Schedule relation value validation
    var rvds = new List<RelationValueValidationData>();
    Scheduler.Add(() => rvds.AddRange(new RelationValueTraveller(ctx, processFiles).Run().SelectMany(x => x)));
    Scheduler.Add(() => new RelationValueValidator(ctx, rvds).Run());
    Scheduler.Add(() => new StrongTypeValidator(ctx, processFiles).Run());

    // Execute all scheduled validation tasks
    Scheduler.Run();

    Logger.NewLine();
    Logger.NewLine();

    // Check if validation completed successfully
    var isComplete = !Scheduler.Suspended;
    if (!isComplete)
        Logger.WriteLine("테이블 변환 과정에서 에러가 발생했습니다.", foreground: ConsoleColor.Red, decorate: false);
    else
        Logger.WriteLine("테이블 변환과 검증을 완료했습니다.", foreground: ConsoleColor.Blue, decorate: false);

    // Reset logger and scheduler for code generation phase
    Logger.Reset();
    Scheduler.Reset();

    // If validation successful, proceed with code generation
    if (isComplete)
    {
        // Generate JSON files
        Scheduler.Add(() => new JsonFileGenerator(ctx).Run());
        if (languages.Split('|').Contains("go"))
            Scheduler.Add(() => new HasAJsonFileGenerator(ctx).Run());

        Scheduler.Add(() => new DiffFileGenerator(ctx).Run());

        // Generate code for each specified language
        foreach (var lang in languages.Split('|').Select(x => x.Trim().ToLower()).Distinct().ToHashSet())
        {
            switch (lang)
            {
                case "c++":
                    Scheduler.Add(() => new ExcelTableConverter.Worker.Generator.CPP.ClassFileGenerator(ctx).Run());
                    break;

                case "c#":
                    Scheduler.Add(() => new ExcelTableConverter.Worker.Generator.CS.ClassFileGenerator(ctx).Run());
                    break;

                case "node":
                    Scheduler.Add(() => new ExcelTableConverter.Worker.Generator.Node.ClassFileGenerator(ctx).Run());
                    break;

                case "go":
                    Scheduler.Add(() => new ExcelTableConverter.Worker.Generator.Go.ClassFileGenerator(ctx).Run());
                    break;
            }
        }
        
        // Generate CRC files for integrity checking
        Scheduler.Add(() =>
        {
            foreach (var scope in new[] { Scope.Server, Scope.Client })
            {
                var jsondir = Path.Combine(ctx.Output, Context.Config.JsonFilePath, $"{scope}".ToLower());
                var crc32 = Directory.GetFiles(jsondir, "*.json").ToDictionary(path => Path.GetFileName(path), path =>
                {
                    var bytes = File.ReadAllBytes(path);
                    var crc = Crc32Algorithm.Compute(bytes);
                    return crc;
                });

                File.WriteAllText(Path.Combine(jsondir, "Crc.txt"), JsonConvert.SerializeObject(crc32));
            }
            Logger.Complete($"CRC 파일을 생성했습니다.");
        });
    }
    
    // Schedule data cache update if values were processed
    if (isCastValues)
        Scheduler.Add(() => new DataCacheWorker(ctx, updatedFiles).Run());

    // Execute all scheduled generation tasks
    Scheduler.Run();

    // Write performance metrics and error tracking
    File.WriteAllText("ElapsedTime.txt", ElapsedTimeMeasurer.Display());
    File.WriteAllText(Context.ERROR_CACHE_PATH, JsonConvert.SerializeObject(Logger.ErrorFiles));

    // Exit with error code if processing failed
    if (!isComplete || Scheduler.Suspended)
        Environment.Exit(1);
}
catch (LogicException e)
{
    Logger.Error(e.Message);
    Environment.Exit(1);
}
catch (Exception e)
{
    // Handle and log all exceptions in the exception hierarchy
    var queue = new Stack<Exception>();
    queue.Push(e);
    while (queue.TryPop(out var error))
    {
        if (error is AggregateException aggregateException)
        {
            foreach (var innerException in aggregateException.InnerExceptions)
            {
                queue.Push(innerException);
            }
        }

        Logger.Error(error.GetType().Name);
        Logger.Error(error.Message);
        Logger.Error(error.StackTrace);
        Environment.Exit(1);
    }
}
