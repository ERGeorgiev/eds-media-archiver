using EdsMediaArchiver.Helpers;
using EdsMediaArchiver.Services;
using EdsMediaArchiver.Services.Processors;
using EdsMediaArchiver.Services.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

Console.WriteLine();
Console.WriteLine($"  Ed's Media Archiver v{Assembly.GetEntryAssembly()?.GetName().Version?.ToString(2)}");
Console.WriteLine("  Prepare your media files for storage.");
Console.WriteLine();

// Validate Input
if (args.Length == 0)
{
    if (Debugger.IsAttached)
    {
        var solutionDir = new DirectoryInfo(Directory.GetCurrentDirectory()).Parent!.Parent!.Parent!.FullName;
        args = [$"{Path.Combine(solutionDir, "TestData")}"];
    }
    else
    {
        Console.Error.WriteLine("[ERROR] No input provided");
        Console.WriteLine("To use, just drop files or folders to be processed (recursively) on top of this .exe file.");
        Console.Write("Press any key to exit...");
        Console.ReadLine();
        return 1;
    }
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
Console.WriteLine();

// Services
var serviceProvider = ServiceProviderHelper.Create(prefs);
var filExtensionResolver = serviceProvider.GetRequiredService<IFileExtensionResolver>();
var fileRequestFactory = serviceProvider.GetRequiredService<IArchiveRequestFactory>();
var compressProcessor = serviceProvider.GetRequiredService<ICompressProcessor>();

// Process each folder
ConcurrentBag<string> errors = [];
foreach (var inputPath in args)
{
    Console.WriteLine();
    Console.WriteLine($"  Processing: {inputPath}");

    var isDir = Directory.Exists(inputPath);
    var isFile = File.Exists(inputPath);
    if (isDir == false && isFile == false)
    {
        var msg = $"    [ERROR] Target not found: {inputPath}";
        errors.Add(msg);
        Console.Error.WriteLine(msg);
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
    Console.WriteLine($"    Found {files.Count} files");

    await Parallel.ForEachAsync(files, new ParallelOptions() { MaxDegreeOfParallelism = 2 }, async (filePath, ct) =>
    {
        await Task.Run(async () => {
            var fileRelativePath = Path.GetRelativePath(dirPath, filePath);
            try
            {
                var newPath = await filExtensionResolver.RestoreExtension(filePath);
                if (filePath.Equals(newPath, StringComparison.OrdinalIgnoreCase) == false)
                {
                    var newRelativePath = Path.GetRelativePath(dirPath, newPath);
                    Console.WriteLine($"    [RENAME] File '{fileRelativePath}' to '{newRelativePath}'");
                    filePath = newPath;
                    fileRelativePath = newRelativePath;
                }

                var compressed = await compressProcessor.ProcessAsync(filePath);
                if (compressed)
                    Console.WriteLine($"    [PROCESSED] File '{fileRelativePath}'");
                else
                    Console.WriteLine($"    [SKIP] File '{fileRelativePath}' has no need for processing");
            }
            catch (Exception e)
            {
                var msg = $"    [ERROR] File '{Path.GetRelativePath(dirPath, filePath)}': {e.Message}";
                errors.Add(msg);
                Console.Error.WriteLine(msg);
            }
        }, ct);
    });

    Console.WriteLine($"    Finished Processing: {inputPath}");
    Console.WriteLine();
}

Console.WriteLine("  All done!");
Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Red;
Console.WriteLine($"  Errors:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
Console.ResetColor();
Console.WriteLine();
Console.Write("Press Enter to exit...");
ConsoleHelper.FlushInput();
Console.ReadLine();

return 0;
