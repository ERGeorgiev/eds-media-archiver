using EdsMediaArchiver.Models;

namespace EdsMediaArchiver.Services;

/// <summary>
/// Processes a single media file: detects type, fixes extension, resolves best date,
/// converts XMP-only formats if needed, and writes metadata + filesystem dates.
/// </summary>
public class MediaFileProcessor(ImageMagickService magick, MetadataService metadataService, DateResolver dateResolver)
{
    public async Task<ProcessingResult> ProcessFileAsync(string rootPath, string filePath)
    {
        var relativePath = Path.GetRelativePath(rootPath, filePath);

        try
        {
            // Detect actual file type via magic bytes (not just the extension)
            var actualType = metadataService.DetectFileType(filePath);

            // Fix mislabeled extension if needed
            filePath = FixExtension(filePath, actualType, rootPath, ref relativePath);

            // Determine the best available date
            var bestDate = dateResolver.ResolveBestDate(filePath);
            if (!bestDate.HasValue)
            {
                Console.WriteLine($"  [SKIP] {relativePath} - no valid dates found");
                return new ProcessingResult(relativePath, null, ProcessingStatus.Skipped, "No valid dates found");
            }

            // XMP-only formats: convert to JPG via Magick.NET
            if (MediaType.XmpOnlyTypes.Contains(actualType))
            {
                return await ConvertAndWriteDateAsync(filePath, bestDate.Value, rootPath, relativePath);
            }

            // Write date metadata based on file type
            await WriteDateForTypeAsync(filePath, actualType, bestDate.Value);

            // Set filesystem Created/Modified dates
            SetFilesystemDates(filePath, bestDate.Value);

            Console.WriteLine($"  [OK] {relativePath} -> {bestDate.Value:yyyy-MM-dd HH:mm:ss}");
            return new ProcessingResult(relativePath, bestDate.Value, ProcessingStatus.Fixed);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [ERR] {relativePath} - {ex.Message}");
            return new ProcessingResult(relativePath, null, ProcessingStatus.Error, ex.Message);
        }
    }

    private async Task<ProcessingResult> ConvertAndWriteDateAsync(
        string filePath, DateTimeOffset bestDate, string rootPath, string relativePath)
    {
        var jpgPath = Path.ChangeExtension(filePath, ".jpg");
        if (File.Exists(jpgPath) && !string.Equals(jpgPath, filePath, StringComparison.OrdinalIgnoreCase))
            jpgPath = GetUniqueFilePath(jpgPath, "-converted");

        var success = await magick.ConvertToJpgAsync(filePath, jpgPath);
        if (!success)
        {
            Console.WriteLine($"  [ERR] {relativePath} - Magick.NET conversion failed, falling back to XMP");
            await metadataService.WriteXmpDatesAsync(filePath, bestDate);
            SetFilesystemDates(filePath, bestDate);
            return new ProcessingResult(relativePath, bestDate, ProcessingStatus.Fixed);
        }

        // Write EXIF dates into the new JPG
        await metadataService.WriteExifDatesAsync(jpgPath, bestDate);
        SetFilesystemDates(jpgPath, bestDate);

        // Delete original (conversion succeeded)
        File.Delete(filePath);

        var jpgRelative = Path.GetRelativePath(rootPath, jpgPath);
        Console.WriteLine($"  [CONV] {relativePath} -> {jpgRelative} ({bestDate:yyyy-MM-dd HH:mm:ss})");
        return new ProcessingResult(relativePath, bestDate, ProcessingStatus.Converted);
    }

    private static string FixExtension(string filePath, string actualType, string rootPath, ref string relativePath)
    {
        if (!Constants.FileTypeToExtension.TryGetValue(actualType, out var correctExt))
            return filePath;

        var currentExt = Path.GetExtension(filePath);
        var normCurrent = NormalizeExtension(currentExt);
        var normCorrect = NormalizeExtension(correctExt);

        if (normCurrent.Equals(normCorrect, StringComparison.OrdinalIgnoreCase))
            return filePath;

        var newPath = Path.ChangeExtension(filePath, correctExt);
        newPath = GetUniqueFilePath(newPath, "-renamed");

        File.Move(filePath, newPath);

        var newRelativePath = Path.GetRelativePath(rootPath, newPath);
        Console.WriteLine($"  [RENAME] {relativePath} -> {Path.GetFileName(newPath)} (actual type: {actualType})");
        relativePath = newRelativePath;
        return newPath;
    }

    private async Task WriteDateForTypeAsync(string filePath, string actualType, DateTimeOffset date)
    {
        if (MediaType.ExifWritableTypes.Contains(actualType))
        {
            await metadataService.WriteExifDatesAsync(filePath, date);
        }
        else if (MediaType.VideoTypes.Contains(actualType))
        {
            metadataService.WriteVideoDates(filePath, date);
        }
        else if (actualType.Equals("PNG", StringComparison.OrdinalIgnoreCase))
        {
            await metadataService.WritePngDatesAsync(filePath, date);
        }
        else
        {
            // Unknown type — XMP as best effort
            await metadataService.WriteXmpDatesAsync(filePath, date);
        }
    }

    private static void SetFilesystemDates(string filePath, DateTimeOffset date)
    {
        File.SetCreationTime(filePath, date.LocalDateTime);
        File.SetLastWriteTime(filePath, date.LocalDateTime);
    }

    private static string NormalizeExtension(string ext)
    {
        return ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ? ".jpg" : ext.ToLowerInvariant();
    }

    private static string GetUniqueFilePath(string path, string suffix)
    {
        if (!File.Exists(path)) return path;

        var dir = Path.GetDirectoryName(path)!;
        var baseName = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);
        var counter = 1;

        string candidate;
        do
        {
            candidate = Path.Combine(dir, $"{baseName}{suffix}{counter}{ext}");
            counter++;
        } while (File.Exists(candidate));

        return candidate;
    }
}
