using EdsMediaArchiver.Models;
using EdsMediaArchiver.Services.Converters;

namespace EdsMediaArchiver.Services.Processors;

public interface ICompressProcessor
{
    /// <summary>
    /// Converts XMP-only format files to JPG. Writes dates if available.
    /// Returns null if not applicable (file is not an XMP-only type).
    /// </summary>
    Task<ProcessingResult?> ProcessAsync(ArchiveRequest request, string actualType);
}

public class CompressProcessor(IImageConverter imageConverter, IMetadataWriter metadataWriter) : ICompressProcessor
{
    public async Task<ProcessingResult?> ProcessAsync(ArchiveRequest request, string actualType)
    {
        // Only applicable to XMP-only types — formats that can't store EXIF dates natively
        if (!MediaType.XmpOnlyTypes.Contains(actualType))
            return null;

        var filePath = request.NewPath.Absolute;
        var rootPath = request.NewPath.Root;
        var relativePath = request.NewPath.Relative;

        var jpgPath = Path.ChangeExtension(filePath, ".jpg");
        if (File.Exists(jpgPath) && !string.Equals(jpgPath, filePath, StringComparison.OrdinalIgnoreCase))
            jpgPath = GetUniqueFilePath(jpgPath);

        var success = await imageConverter.ConvertToJpgAsync(filePath, jpgPath);
        if (!success)
        {
            if (request.OriginDate.HasValue)
            {
                Console.WriteLine($"  [ERR] {relativePath} - conversion failed, falling back to XMP dates");
                await metadataWriter.WriteXmpDatesAsync(filePath, request.OriginDate.Value);
                SetFilesystemDates(filePath, request.OriginDate.Value);
                return new ProcessingResult(relativePath, request.OriginDate.Value, ProcessingStatus.Fixed);
            }

            Console.WriteLine($"  [ERR] {relativePath} - conversion failed");
            return new ProcessingResult(relativePath, null, ProcessingStatus.Error, "Conversion failed");
        }

        // Write dates to the new JPG if available
        if (request.OriginDate.HasValue)
        {
            await metadataWriter.WriteExifDatesAsync(jpgPath, request.OriginDate.Value);
            SetFilesystemDates(jpgPath, request.OriginDate.Value);
        }

        File.Delete(filePath);

        var jpgRelative = Path.GetRelativePath(rootPath, jpgPath);
        var dateDisplay = request.OriginDate.HasValue
            ? $" ({request.OriginDate.Value:yyyy-MM-dd HH:mm:ss})"
            : "";
        Console.WriteLine($"  [CONV] {relativePath} -> {jpgRelative}{dateDisplay}");
        request.NewPath = new PathInfo(rootPath, jpgPath);
        return new ProcessingResult(relativePath, request.OriginDate, ProcessingStatus.Converted);
    }

    private static void SetFilesystemDates(string filePath, DateTimeOffset date)
    {
        File.SetCreationTime(filePath, date.LocalDateTime);
        File.SetLastWriteTime(filePath, date.LocalDateTime);
    }

    private static string GetUniqueFilePath(string path)
    {
        if (!File.Exists(path)) return path;

        var dir = Path.GetDirectoryName(path)!;
        var baseName = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);
        var counter = 1;

        string candidate;
        do
        {
            candidate = Path.Combine(dir, $"{baseName}{counter}{ext}");
            counter++;
        } while (File.Exists(candidate));

        return candidate;
    }
}
