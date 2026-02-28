using EdsMediaArchiver;
using EdsMediaArchiver.Models;
using EdsMediaArchiver.Services;
using EdsMediaArchiver.Services.Converters;
using EdsMediaArchiver.Services.FileDateReaders;
using EdsMediaArchiver.Services.Processors;
using EdsMediaArchiver.Services.Resolvers;
using EdsMediaArchiver.Services.Validators;
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
    args = [$"{Path.Combine(Directory.GetCurrentDirectory(), "TestData")}", $"{Path.Combine(Directory.GetCurrentDirectory(), "TestData2")}"];

// Validate input paths
if (args.Length == 0)
{
    Console.Error.WriteLine("[ERROR] No folders provided. Pass folder paths as arguments.");
    Console.Error.WriteLine("To use, just drop files or folders to be processed (recursively) on top of this .exe file.");
    Console.Write("Press any key to exit...");
    Console.ReadLine();
    return 1;
}

// Ask user preferences
Console.WriteLine("  What would you like to do?");
Console.WriteLine();

Console.WriteLine("  Fix file extensions?");
Console.WriteLine("    Detects actual file types and renames mislabeled extensions.");
Console.Write("    (Y/n): ");
var fixExtensions = AskYesNo();
Console.WriteLine();

Console.WriteLine("  Compress files?");
Console.WriteLine("    Converts formats without EXIF support (WebP, BMP, GIF, TIFF) to JPG.");
Console.WriteLine("    Note: Dates are automatically written to compressed files.");
Console.Write("    (Y/n): ");
var compress = AskYesNo();
Console.WriteLine();

Console.WriteLine("  Set file dates?");
Console.WriteLine("    Writes date metadata and sets filesystem Created/Modified dates.");
if (compress)
    Console.WriteLine("    Applies to files not already handled by compression.");
Console.Write("    (Y/n): ");
var setDates = AskYesNo();

if (!fixExtensions && !compress && !setDates)
{
    Console.WriteLine();
    Console.WriteLine("  No options selected. Nothing to do.");
    Console.Write("Press any key to exit...");
    Console.ReadLine();
    return 0;
}

var preferences = new UserPreferences(fixExtensions, compress, setDates);

Console.WriteLine();
Console.WriteLine("  Selected options:");
if (fixExtensions) Console.WriteLine("    - Fix file extensions");
if (compress)      Console.WriteLine("    - Compress files (with date metadata)");
if (setDates)      Console.WriteLine("    - Set file dates");

// ToDo: Verify backup folder created

Console.WriteLine();
Console.Write("  Proceed? (Y/n): ");
if (!AskYesNo())
{
    Console.WriteLine("  Cancelled.");
    Console.Write("Press any key to exit...");
    Console.ReadLine();
    return 0;
}

// 1. Setup the container
var serviceProvider = new ServiceCollection()
    .AddSingleton<IDateValidator, DateValidator>()
    .AddSingleton<IMetadataWriter, MetadataWriter>()
    .AddSingleton<IOriginalDateReader, OriginalDateReader>()
    .AddSingleton<IFilenameDateReader, FilenameDateReader>()
    .AddSingleton<IOldestDateReader, OldestDateReader>()
    .AddSingleton<IFileDateResolver, FileDateResolver>()
    .AddSingleton<IFileTypeResolver, FileTypeResolver>()
    .AddSingleton<IImageConverter, ImageConverter>()
    .AddSingleton<IExtensionFixProcessor, ExtensionFixProcessor>()
    .AddSingleton<ICompressProcessor, CompressProcessor>()
    .AddSingleton<IDateProcessor, DateProcessor>()
    .AddSingleton<IArchiveProcessor, ArchiveProcessor>()
    .AddSingleton<IArchiveRequestFactory, ArchiveRequestFactory>()
    .BuildServiceProvider();

// Services
var mediaFileProcessor = serviceProvider.GetRequiredService<IArchiveProcessor>();
var fileRequestFactory = serviceProvider.GetRequiredService<IArchiveRequestFactory>();

// Process each folder
foreach (var inputPath in args)
{
    Console.WriteLine("────────────────────────────────────────────────");
    Console.WriteLine($"Processing: {inputPath}");
    Console.WriteLine();

    var targetInfo = new FileInfo(inputPath);
    var exists = (int)targetInfo.Attributes != -1;
    if (exists == false)
    {
        Console.Error.WriteLine($"[ERROR] Target not found: {inputPath}");
        continue;
    }

    string dirPath;
    List<string> files;
    if (targetInfo.Attributes.HasFlag(FileAttributes.Directory)) // It's a directory
    {
        dirPath = inputPath;
        files = Directory.EnumerateFiles(inputPath, "*", SearchOption.AllDirectories).ToList();
    }
    else // It's a file
    {
        dirPath = Path.GetDirectoryName(inputPath) ?? "";
        files = [inputPath];
    }
    var unsupportedFiles = files.Where(f => Constants.SupportedExtensions.Contains(Path.GetExtension(f)) == false).ToList(); // ToDo: Decide with actual type
    var supportedFiles = files.Except(unsupportedFiles).ToList();
    Console.WriteLine($"  Found {supportedFiles.Count} supported files");
    Console.WriteLine($"  Found {unsupportedFiles.Count} unsupported files");
    Console.WriteLine();

    // Process files with limited parallelism
    var semaphore = new SemaphoreSlim(10);
    var tasks = supportedFiles.Select(async file =>
    {
        await semaphore.WaitAsync();
        try
        {
            var request = fileRequestFactory.Create(dirPath, file);
            request.FixExtension = preferences.FixExtensions;
            request.Compress = preferences.Compress;
            request.SetDates = preferences.SetDates;
            return await mediaFileProcessor.ProcessFileAsync(request);
        }
        finally
        {
            semaphore.Release();
        }
    });

    var results = await Task.WhenAll(tasks);
    Console.WriteLine($"  Finished Processing: {inputPath}");

    // Summary
    Console.WriteLine("────────────────────────────────────────────────");
    Console.WriteLine($"Summary");
    var fixedFiles = results.Where(r => r.Status == ProcessingStatus.Fixed).ToList();
    var convertedFiles = results.Where(r => r.Status == ProcessingStatus.Converted).ToList();
    var renamedFiles = results.Where(r => r.Status == ProcessingStatus.Renamed).ToList();
    var skippedFiles = results.Where(r => r.Status == ProcessingStatus.Skipped).ToList();
    var errorFiles = results.Where(r => r.Status == ProcessingStatus.Error).ToList();

    if (fixedFiles.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("  Date Fixes:");
        foreach (var r in fixedFiles)
            Console.WriteLine($"    {r.RelativePath,-60} {r.DateAssigned:yyyy-MM-dd HH:mm:ss}");
    }

    if (unsupportedFiles.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("  Unsupported:");
        foreach (var r in unsupportedFiles)
            Console.WriteLine($"    {Path.GetRelativePath(dirPath, r),-60}");
    }

    if (convertedFiles.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("  Converted to JPG:"); // todo: to converted any to any
        foreach (var r in convertedFiles)
            Console.WriteLine($"    {r.RelativePath,-60} {r.DateAssigned:yyyy-MM-dd HH:mm:ss}");
    }

    if (renamedFiles.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("  Renamed (extension fixed):");
        foreach (var r in renamedFiles)
            Console.WriteLine($"    {r.RelativePath,-60}");
    }

    if (skippedFiles.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("  Skipped:");
        foreach (var r in skippedFiles)
            Console.WriteLine($"    {r.RelativePath,-60} {r.ErrorMessage}");
    }

    if (errorFiles.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("  Errors:");
        foreach (var r in errorFiles)
            Console.WriteLine($"    {r.RelativePath,-60} {r.ErrorMessage}");
    }

    Console.WriteLine();
    Console.WriteLine("  Results:");
    Console.WriteLine($"    Processed:  {fixedFiles.Count}");
    Console.WriteLine($"    Converted:  {convertedFiles.Count}");
    Console.WriteLine($"    Renamed:    {renamedFiles.Count}");
    Console.WriteLine($"    Skipped:    {skippedFiles.Count}");
    Console.WriteLine($"    Errors:     {errorFiles.Count}");
}

Console.WriteLine();
Console.WriteLine("================================================");
Console.WriteLine("  All done!");
Console.WriteLine("================================================");
Console.WriteLine();
Console.Write("Press any key to exit...");
Console.ReadLine();

return 0;

static bool AskYesNo()
{
    var input = Console.ReadLine()?.Trim();
    return string.IsNullOrEmpty(input) || input.StartsWith("y", StringComparison.OrdinalIgnoreCase);
}
