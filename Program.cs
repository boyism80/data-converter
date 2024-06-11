using ExcelTableConverter;
using ExcelTableConverter.Model;
using ExcelTableConverter.Util;
using ExcelTableConverter.Worker;
using ExcelTableConverter.Worker.Cache;
using ExcelTableConverter.Worker.Generator;
using ExcelTableConverter.Worker.Generator.CS;
using ExcelTableConverter.Worker.Loader;
using ExcelTableConverter.Worker.Validator;
using Force.Crc32;
using NDesk.Options;
using Newtonsoft.Json;
using Scriban;

try
{
    var dir = Path.Combine("..", "..", "..", "..");
    var options = new OptionSet
    {
        { "d|dir=", "input directory", v => dir = v }
    };

    options.Parse(args);
#if !JENKINS
    Console.Clear();
#endif

    var cached = File.Exists(Context.RAW_CACHE_PATH) ? ZipUtil.Unzip<Context>(File.ReadAllBytes(Context.RAW_CACHE_PATH)) : new Context();
    if (cached.BuildVersion != Context.BUILD_VERSION)
    {
        cached = new Context();
        if (Directory.Exists(Context.CACHE_DIRECTORY))
        {
            foreach (var file in Directory.GetFiles(Context.CACHE_DIRECTORY, "*", SearchOption.TopDirectoryOnly))
                File.Delete(file);
        }

        Logger.Write(" 컨버터 빌드 버전이 변경되어 캐시파일을 전부 제거했습니다.", false, false, foreground: ConsoleColor.Blue);
    }
    var loaded = new Context();

    var enumFiles = new List<string>();
    var constFiles = new List<string>();
    var dataFiles = new List<string>();
    var paths = Directory.GetFiles(dir, "*.xlsx", SearchOption.TopDirectoryOnly);
    foreach (var p in paths)
    {
        try
        {
            if (Path.GetFileName(p).StartsWith("~$"))
                continue;

            var bytes = File.ReadAllBytes(p);
            var crc = $"{Crc32Algorithm.Compute(bytes)}.{bytes.Length}";
            var fname = Path.GetFileName(p);
            if (cached.CRC.TryGetValue(fname, out var old) && old == crc)
                continue;

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
            throw new LogicException($"{Path.GetFileName(p)} 파일을 열 수 없습니다.");
        }
        catch (Exception e)
        {
            throw new AggregateException($"{Path.GetFileName(p)} 파일을 여는 과정에서 문제가 발생했습니다.", e);
        }
    }

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

    var errorFiles = File.Exists(Context.ERROR_CACHE_PATH) ? JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(Context.ERROR_CACHE_PATH)) : new List<string>();

    foreach (var fileName in deletedFiles.Concat(updatedFiles).Concat(errorFiles))
    {
        var cacheFilePath = Context.GetCacheFilePath(fileName);
        if (File.Exists(cacheFilePath))
        {
            File.Delete(cacheFilePath);
        }
    }

    Logger.Next();
    if (updatedFiles.Concat(errorFiles).Any())
    {
        Logger.Write(" 변경된 파일 또는 가장 마지막 에러 발생 파일에 대해서만 작업을 진행합니다.", false, false, foreground: ConsoleColor.Blue);

        foreach (var (files, suffix) in new[] { (updatedFiles, "변경된 파일"), (errorFiles, "에러 파일") })
        {
            if (files.Count == 0)
                continue;

            var fileName = files[0];
            var anotherFiles = files.Skip(1).ToList();
            var message = fileName;
            if (anotherFiles.Count > 0)
                message = $"{fileName} 외 {anotherFiles.Count}개 파일";
            Logger.Append($"{message} ({suffix})", ConsoleColor.DarkGray);
        }
    }
    else
    {
        Logger.Write(" 변경된 파일 또는 가장 마지막 에러 발생 파일이 없습니다.", false, false, foreground: ConsoleColor.Blue);
    }
    Logger.Next(2);

    Scheduler.Add(() =>
    {
        var constWorkBooks = new ExcelFileLoader(loaded, constFiles, quiet: true).Run();
        var constSheets = new SheetLoader(loaded, constWorkBooks, quiet: true).Run();
        new RawConstLoader(loaded, constSheets).Run();
    });

    Scheduler.Add(() =>
    {
        var enumWorkBooks = new ExcelFileLoader(loaded, enumFiles, quiet: true).Run();
        var enumSheets = new SheetLoader(loaded, enumWorkBooks, quiet: true).Run();
        new RawEnumLoader(loaded, enumSheets).Run();
    });

    IReadOnlyList<Workbook> dataWorkBooks = null;
    Scheduler.Add(() =>
    {
        dataWorkBooks = new ExcelFileLoader(loaded, dataFiles).Run();
    });


    IReadOnlyList<Sheet> dataSheets = null;
    Scheduler.Add(() =>
    {
        dataSheets = new SheetLoader(loaded, dataWorkBooks).Run();
    });

    var ctx = new Context();
    Scheduler.Add(() =>
    {
        new RawDataLoader(loaded, dataSheets).Run();
        ctx = cached + loaded;

        if (File.Exists(Context.RAW_CACHE_PATH))
            File.Delete(Context.RAW_CACHE_PATH);

        File.WriteAllBytes(Context.RAW_CACHE_PATH, ZipUtil.Zip(ctx));
    });



    var isCastValues = false;
    Scheduler.Add(() =>
    {
        ctx.Arrange();
        isCastValues = true;
    }, stopOnError: true);

    Scheduler.Add(() => new NameValidator(ctx).Run());
    Scheduler.Add(() => new SchemaValidator(ctx).Run());
    Scheduler.Add(() => new KeyValidator(ctx).Run());
    Scheduler.Add(() => new DslValidator(ctx).Run());
    Scheduler.Add(() => new RelationTypeValidator(ctx).Run());

    var rvds = new List<RelationValueValidationData>();
    Scheduler.Add(() => rvds.AddRange(new RelationValueTraveller(ctx, updatedFiles.Concat(errorFiles)).Run().SelectMany(x => x)));
    Scheduler.Add(() => new RelationValueValidator(ctx, rvds).Run());
    Scheduler.Add(() => new StrongTypeValidator(ctx, updatedFiles.Concat(errorFiles)).Run());

    Scheduler.Run();

    Logger.Next(2);

    var isComplete = !Scheduler.Suspended;
    if (!isComplete)
        Logger.Write("테이블 변환 과정에서 에러가 발생했습니다.", false, false, foreground: ConsoleColor.Red);
    else
        Logger.Write("테이블 변환과 검증을 완료했습니다.", false, false, foreground: ConsoleColor.Blue);

    Logger.Reset();
    Scheduler.Reset();

    if (isComplete)
    {
        Scheduler.Add(() => new JsonFileGenerator(ctx).Run());
        Scheduler.Add(() => new DiffFileGenerator(ctx).Run());
        Scheduler.Add(() => new ExcelTableConverter.Worker.Generator.CPP.ClassFileGenerator(ctx).Run());
        Scheduler.Add(() => new ExcelTableConverter.Worker.Generator.CPP.BindFileGenerator(ctx).Run());
        Scheduler.Add(() =>
        {
            foreach (var scope in new[] { Scope.Server, Scope.Client })
            {
                var result = new Dictionary<string, Dictionary<string, object>>();
                foreach (var (tableName, constSet) in ctx.Result.Const.OrderBy(x => x.Key))
                {
                    var buffer = new Dictionary<string, object>();
                    foreach (var constValue in constSet.Values)
                    {
                        if (constValue.Scope.HasFlag(scope) == false)
                            continue;

                        buffer.Add(constValue.Name, constValue.Value);
                    }
                    result.Add($"___{tableName}", buffer);
                }

                foreach (var p in new[] { Context.Config.JsonFilePath, Context.Config.JsonSheetFilePath })
                {
                    var path = Path.Combine(ctx.Output, p, $"{scope}".ToLower(), "const.json");
                    if (Directory.Exists(Path.GetDirectoryName(path)) == false)
                        continue;

                    File.WriteAllText(path, JsonConvert.SerializeObject(result, Formatting.Indented));
                }
            }
            new ExcelTableConverter.Worker.Generator.CPP.ConstFileGenerator(ctx).Run();
        });
        Scheduler.Add(() => new ExcelTableConverter.Worker.Generator.CPP.EnumFileGenerator(ctx).Run());
        Scheduler.Add(() => new CMPResolverGenerator(ctx).Run());
        Scheduler.Add(() => new DslFileGenerator(ctx).Run());
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
        Scheduler.Add(() =>
        {
            var template = Template.Parse(File.ReadAllText($"Template/readme.txt"));
            var dslPrototypes = JsonConvert.DeserializeObject<Dictionary<string, List<DSLParameter>>>(File.ReadAllText("dsl.json"));
            var code = template.Render(new { Dsl = dslPrototypes });

            var path = Path.Combine(ctx.Output, "readme");
            if (Directory.Exists(path) == false)
                Directory.CreateDirectory(path);

            path = Path.Combine(path, "README.md");
            File.WriteAllText(path, code);
            Logger.Complete($"README 파일을 생성했습니다.");
        });
    }
    if (isCastValues)
        Scheduler.Add(() => new DataCacheWorker(ctx, updatedFiles).Run());

    Scheduler.Run();

    File.WriteAllText("ElapsedTime.txt", ElapsedTimeMeasurer.Display());
    File.WriteAllText(Context.ERROR_CACHE_PATH, JsonConvert.SerializeObject(Logger.ErrorFiles));

    if (isComplete == false)
        Environment.Exit(1);
}
catch (LogicException e)
{
    Logger.Error(e.Message);
    Environment.Exit(1);
}
catch (Exception e)
{
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
