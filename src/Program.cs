using EdsMediaArchiver.Helpers;
using EdsMediaArchiver.Services;
using EdsMediaArchiver.Services.Logging;
using EdsMediaArchiver.Services.Processors;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Reflection;

Console.WriteLine();
Console.WriteLine("================================================");
Console.WriteLine($"  Ed's Media Archiver v{Assembly.GetEntryAssembly()?.GetName().Version?.ToString(2)}");
Console.WriteLine("  Prepare your media files for storage.");
Console.WriteLine("================================================");
Console.WriteLine();

if (Debugger.IsAttached)
{
    var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
    var solutionDir = currentDirectory.Parent!.Parent!.Parent!.FullName;
    args = [$"{Path.Combine(solutionDir, "TestData")}"];
}

// Validate input paths
if (args.Length == 0)
{
    Console.Error.WriteLine("[ERROR] No folders provided. Pass folder paths as arguments.");
    Console.WriteLine("To use, just drop files or folders to be processed (recursively) on top of this .exe file.");
    Console.Write("Press any key to exit...");
    Console.ReadLine();
    return 1;
}

Console.WriteLine($"  Input:");
Console.WriteLine($"    {string.Join(Environment.NewLine + "    ", args)}");
Console.WriteLine();

if (ConsoleHelper.TryGetUserPreferences(out var prefs) == false)
{
    Console.Write("Press any key to exit...");
    Console.ReadLine();
}

Console.WriteLine();
Console.Write("  Ensure you have created a backup! Proceed? (Y/n): ");
ConsoleHelper.FlushInput();
if (ConsoleHelper.AskYesNo() == false)
{
    Console.WriteLine("  Cancelled.");
    Console.Write("Press any key to exit...");
    Console.ReadLine();
    return 0;
}

// Services
var serviceProvider = ServiceProviderHelper.Create();
var mediaFileProcessor = serviceProvider.GetRequiredService<IArchiveProcessor>();
var fileRequestFactory = serviceProvider.GetRequiredService<IArchiveRequestFactory>();
var logs = serviceProvider.GetRequiredService<IProcessLogger>();

// Process each folder
foreach (var inputPath in args)
{
    Console.WriteLine("────────────────────────────────────────────────");
    Console.WriteLine($"Processing: {inputPath}");
    Console.WriteLine();

    var isDir = Directory.Exists(inputPath);
    var isFile = File.Exists(inputPath);
    if (isDir == false && isFile == false)
    {
        Console.Error.WriteLine($"[ERROR] Target not found: {inputPath}");
        continue;
    }

    string dirPath;
    List<string> files;
    if (isDir) // It's a directory
    {
        dirPath = inputPath;
        files = Directory.EnumerateFiles(inputPath, "*", SearchOption.AllDirectories).ToList();
    }
    else // It's a file
    {
        dirPath = Path.GetDirectoryName(inputPath) ?? "";
        files = [inputPath];
    }
    Console.WriteLine($"  Found {files.Count} files");
    Console.WriteLine();

    var printLock = new object();
    var options = new ParallelOptions
    {
        MaxDegreeOfParallelism = 1
    };
    await Parallel.ForEachAsync(files, options, async (file, token) =>
    {
        var request = fileRequestFactory.Create(dirPath, file);
        request.Compress = prefs.Compress;
        request.ReizeOnCompress = prefs.ResizeOnCompress;
        request.Standardize = prefs.Standardize;
        request.SetDates = prefs.SetDates;

        await Task.Run(async () => {
            await mediaFileProcessor.ProcessFileAsync(request);
        }, token);

        lock (printLock)
        {
            Console.WriteLine($"FILE: {request.OriginalPath.Absolute}");
            logs.PrintLogs(request.OriginalPath.Absolute);
            if (request.OriginalPath != request.NewPath)
            {
                logs.PrintLogs(request.NewPath.Absolute);
            }
        }
    });

    Console.WriteLine();
    Console.WriteLine($"  Finished Processing: {inputPath}");
}

logs.PrintSummary();

Console.WriteLine();
Console.WriteLine("================================================");
Console.WriteLine("  All done!");
Console.WriteLine("================================================");
Console.WriteLine();
Console.Write("Press any key to exit...");
ConsoleHelper.FlushInput();
Console.ReadLine();

return 0;
