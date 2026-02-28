using EdsMediaArchiver;
using EdsMediaArchiver.Services;
using EdsMediaArchiver.Services.Compressors;
using EdsMediaArchiver.Services.Converters;
using EdsMediaArchiver.Services.FileDateReaders;
using EdsMediaArchiver.Services.Logging;
using EdsMediaArchiver.Services.Processors;
using EdsMediaArchiver.Services.Resolvers;
using EdsMediaArchiver.Services.Validators;
using ImageMagick;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Reflection;

//ToDo: Gifs, Transparent PNGs, Re-compressing JPGs (cases where its wanted and not)

var format = MagickNET.SupportedFormats.First(f => f.Format == MagickFormat.Gif);
var test = FFMpegCore.FFProbe.AnalyseAsync(Path.Combine(Directory.GetCurrentDirectory(), "TestData", "big-gif-converted3.jpg")).Result;

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

Console.WriteLine("  Compress files?");
Console.WriteLine("    Converts media to optimal formats:");
Console.WriteLine("      Images (WebP, BMP, TIFF, GIF) -> JPG (multi-frame GIF -> MP4)");
Console.WriteLine("      Video  (AVI, MKV, WMV, MOV, 3GP) -> MP4 (H.264 + AAC)");
Console.WriteLine("      Audio  (MP3, WAV, FLAC, AAC, WMA, M4A) -> OGG (Vorbis)");
Console.Write("    (Y/n): ");
var preferenceCompress = AskYesNo();
Console.WriteLine();

Console.WriteLine("  Set file dates?");
Console.WriteLine("    Writes date metadata and sets filesystem Created/Modified dates.");
Console.Write("    (Y/n): ");
var preferenceSetDates = AskYesNo();
bool preferenceConvertToSetDate = false;
if (preferenceSetDates && preferenceCompress == false)
{
    Console.WriteLine("  Convert files that cannot realiably have dates set?");
    Console.WriteLine("    Examples: PNG, GIF");
    Console.Write("    (Y/n): ");
    preferenceConvertToSetDate = AskYesNo();
}

if (!preferenceCompress && !preferenceSetDates)
{
    Console.WriteLine();
    Console.WriteLine("  No options selected. Nothing to do.");
    Console.Write("Press any key to exit...");
    Console.ReadLine();
    return 0;
}

Console.WriteLine();
Console.WriteLine("  Selected options:");
if (preferenceCompress)      Console.WriteLine("    - Compress files");
if (preferenceSetDates)      Console.WriteLine("    - Set file dates");
if (preferenceConvertToSetDate)      Console.WriteLine("    - Convert files unreliable for dates");

// ToDo: Verify backup folder created

Console.WriteLine();
Console.Write("  Ensure you have created a backup. Proceed? (Y/n): ");
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
    .AddSingleton<IMediaCompressor, GifCompressor>()
    .AddSingleton<ImageCompressor>()
    .AddSingleton<IMediaCompressor, ImageCompressor>(x => x.GetRequiredService<ImageCompressor>())
    .AddSingleton<IImageCompressor, ImageCompressor>(x => x.GetRequiredService<ImageCompressor>())
    .AddSingleton<VideoCompressor>()
    .AddSingleton<IMediaCompressor, VideoCompressor>(x => x.GetRequiredService<VideoCompressor>())
    .AddSingleton<IVideoCompressor, VideoCompressor>(x => x.GetRequiredService<VideoCompressor>())
    .AddSingleton<IMediaCompressor, AudioCompressor>()
    .AddSingleton<ICompressProcessor, CompressProcessor>()
    .AddSingleton<IMediaConverter, GifConverter>()
    .AddSingleton<ImageConverter>()
    .AddSingleton<IMediaConverter, ImageConverter>(x => x.GetRequiredService<ImageConverter>())
    .AddSingleton<IImageConverter, ImageConverter>(x => x.GetRequiredService<ImageConverter>())
    .AddSingleton<VideoConverter>()
    .AddSingleton<IMediaConverter, VideoConverter>(x => x.GetRequiredService<VideoConverter>())
    .AddSingleton<IVideoConverter, VideoConverter>(x => x.GetRequiredService<VideoConverter>())
    .AddSingleton<IConvertProcessor, ConvertProcessor>()
    .AddSingleton<IDateProcessor, DateProcessor>()
    .AddSingleton<IArchiveProcessor, ArchiveProcessor>()
    .AddSingleton<IArchiveRequestFactory, ArchiveRequestFactory>()
    .BuildServiceProvider();

// Services
var mediaFileProcessor = serviceProvider.GetRequiredService<IArchiveProcessor>();
var fileRequestFactory = serviceProvider.GetRequiredService<IArchiveRequestFactory>();
var logs = serviceProvider.GetRequiredService<IProcessLogger>();

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
    var unsupportedFiles = files.Where(f => MediaType.SupportedTypes.Contains(Path.GetExtension(f)) == false).ToList(); // ToDo: Decide with actual type
    var supportedFiles = files.Except(unsupportedFiles).ToList();
    Console.WriteLine($"  Found {supportedFiles.Count} supported files");
    Console.WriteLine();

    // Process files with limited parallelism
    var semaphore = new SemaphoreSlim(10);
    var tasks = supportedFiles.Select(async file =>
    {
        await semaphore.WaitAsync();
        try
        {
            var request = fileRequestFactory.Create(dirPath, file);
            request.Compress = preferenceCompress;
            request.SetDates = preferenceSetDates;
            request.ConvertIfUnreliableForDates = preferenceConvertToSetDate;
            await mediaFileProcessor.ProcessFileAsync(request);
        }
        finally
        {
            semaphore.Release();
        }
    });

    await Task.WhenAll(tasks);
    Console.WriteLine($"  Finished Processing: {inputPath}");
    logs.PrintSummary();
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
