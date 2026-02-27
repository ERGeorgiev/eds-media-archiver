using PersonalMediaArchiver;
using PersonalMediaArchiver.Models;
using PersonalMediaArchiver.Services;

Console.WriteLine();
Console.WriteLine("================================================");
Console.WriteLine("  Fix-MediaDates v2.0 (C#)");
Console.WriteLine("  Restore true dates to your media files");
Console.WriteLine("================================================");
Console.WriteLine();

// Validate input paths
if (args.Length == 0)
{
    Console.Error.WriteLine("[ERROR] No folders provided. Pass folder paths as arguments.");
    Console.Error.WriteLine("Usage: PersonalMediaArchiver <folder1> [folder2] ...");
    return 1;
}

// Check dependencies
if (!ExifToolService.IsAvailable())
{
    Console.Error.WriteLine("[ERROR] ExifTool is required but not found.");
    Console.Error.WriteLine("  Install via: winget install OliverBetz.ExifTool  OR  choco install exiftool");
    return 1;
}

var hasMagick = ImageMagickService.IsAvailable();

Console.WriteLine("  This tool will:");
Console.WriteLine("    - Detect actual file types and fix wrong extensions");
Console.WriteLine("    - Convert XMP-only images (WebP, BMP, GIF, TIFF) to JPG for EXIF support");
Console.WriteLine("    - Write date metadata and set filesystem dates");

if (!hasMagick)
{
    Console.WriteLine();
    Console.WriteLine("  [WARN] ImageMagick not found. XMP-only formats cannot be converted to JPG.");
    Console.WriteLine("  Install via: winget install ImageMagick.ImageMagick");
    Console.WriteLine("  Those files will receive XMP dates only (may not sort correctly in galleries).");
}

Console.WriteLine();
Console.Write("  Proceed? (Y/n) ");
var confirm = Console.ReadLine();
if (confirm?.TrimStart().StartsWith("n", StringComparison.OrdinalIgnoreCase) == true)
{
    Console.WriteLine("  Cancelled.");
    return 0;
}

// Initialize services
var exifTool = new ExifToolService();
var magick = new ImageMagickService();
var processor = new MediaFileProcessor(exifTool, magick, hasMagick);

// Process each folder
foreach (var inputPath in args)
{
    if (!Directory.Exists(inputPath))
    {
        Console.Error.WriteLine($"[ERROR] Not a valid folder: {inputPath}");
        continue;
    }

    Console.WriteLine("────────────────────────────────────────────────");
    Console.WriteLine($"Processing: {inputPath}");
    Console.WriteLine();

    var files = Directory.EnumerateFiles(inputPath, "*", SearchOption.AllDirectories)
        .Where(f => Constants.SupportedExtensions.Contains(Path.GetExtension(f)))
        .ToList();

    Console.WriteLine($"  Found {files.Count} supported media files");
    Console.WriteLine();

    // Process files with limited parallelism (matches script's ThrottleLimit 10)
    var semaphore = new SemaphoreSlim(10);
    var tasks = files.Select(async file =>
    {
        await semaphore.WaitAsync();
        try
        {
            return await processor.ProcessFileAsync(file, inputPath);
        }
        finally
        {
            semaphore.Release();
        }
    });

    var results = await Task.WhenAll(tasks);

    // Summary
    var fixedFiles    = results.Where(r => r.Status == ProcessingStatus.Fixed).ToList();
    var convertedFiles = results.Where(r => r.Status == ProcessingStatus.Converted).ToList();
    var skippedFiles  = results.Where(r => r.Status == ProcessingStatus.Skipped).ToList();
    var errorFiles    = results.Where(r => r.Status == ProcessingStatus.Error).ToList();

    if (fixedFiles.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("  Date Fixes:");
        foreach (var r in fixedFiles)
            Console.WriteLine($"    {r.RelativePath,-60} {r.DateAssigned:yyyy-MM-dd HH:mm:ss}");
    }

    if (convertedFiles.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("  Converted to JPG:");
        foreach (var r in convertedFiles)
            Console.WriteLine($"    {r.RelativePath,-60} {r.DateAssigned:yyyy-MM-dd HH:mm:ss}");
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
    Console.WriteLine($"    Skipped:    {skippedFiles.Count}");
    Console.WriteLine($"    Errors:     {errorFiles.Count}");
}

Console.WriteLine();
Console.WriteLine("================================================");
Console.WriteLine("  All done!");
Console.WriteLine("================================================");
Console.WriteLine();

return 0;
