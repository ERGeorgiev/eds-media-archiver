using EdsMediaArchiver.Models;
using EdsMediaArchiver.Services.Converters;

namespace EdsMediaArchiver.Services.Processors;

public interface ICompressProcessor
{
    /// <summary>
    /// Fixes the file extension and/or compresses the file based on request flags.
    /// Returns null if no action was taken.
    /// </summary>
    Task<ProcessingResult?> ProcessAsync(ArchiveRequest request);
}

public class CompressProcessor(IEnumerable<IMediaCompressor> compressors) : ICompressProcessor
{
    public async Task<ProcessingResult?> ProcessAsync(ArchiveRequest request)
    {
        var actualType = request.ActualFileType;
        bool wasRenamed = false;

        // 1. Fix extension if requested
        if (request.FixExtension)
            wasRenamed = FixExtension(request, actualType);

        // 2. Compress if requested
        if (request.Compress)
        {
            var compressor = compressors.FirstOrDefault(c => c.IsSupported(actualType));
            if (compressor != null)
            {
                var result = await CompressFileAsync(request, compressor);
                if (result != null)
                    return result;
            }
        }

        // 3. If only extension was fixed, report that
        if (wasRenamed)
            return new ProcessingResult(request.NewPath.Relative, null, ProcessingStatus.Renamed);

        return null;
    }

    private static bool FixExtension(ArchiveRequest request, string actualType)
    {
        if (!Constants.FileTypeToExtension.TryGetValue(actualType, out var correctExt))
            return false;

        var currentExt = Path.GetExtension(request.NewPath.Absolute);
        var normCurrent = NormalizeExtension(currentExt);
        var normCorrect = NormalizeExtension(correctExt);

        if (normCurrent.Equals(normCorrect, StringComparison.OrdinalIgnoreCase))
            return false;

        var oldPath = request.NewPath.Absolute;
        var newPath = Path.ChangeExtension(oldPath, correctExt);
        newPath = GetUniqueFilePath(newPath);

        File.Move(oldPath, newPath);
        request.NewPath = new PathInfo(request.NewPath.Root, newPath);

        Console.WriteLine($"  [RENAME] {request.OriginalPath.Relative} -> {Path.GetFileName(newPath)} (actual type: {actualType})");
        return true;
    }

    private static async Task<ProcessingResult?> CompressFileAsync(ArchiveRequest request, IMediaCompressor compressor)
    {
        var filePath = request.NewPath.Absolute;
        var rootPath = request.NewPath.Root;
        var relativePath = request.NewPath.Relative;
        var outputDir = Path.GetDirectoryName(filePath)!;

        var outputPath = await compressor.CompressAsync(filePath, outputDir);
        if (outputPath == null)
        {
            Console.WriteLine($"  [ERR] {relativePath} - compression failed");
            return new ProcessingResult(relativePath, null, ProcessingStatus.Error, "Compression failed");
        }

        // Delete the original file (compressor creates a new one)
        if (!string.Equals(filePath, outputPath, StringComparison.OrdinalIgnoreCase))
            File.Delete(filePath);

        var outputRelative = Path.GetRelativePath(rootPath, outputPath);
        Console.WriteLine($"  [CONV] {relativePath} -> {outputRelative}");
        request.NewPath = new PathInfo(rootPath, outputPath);
        return new ProcessingResult(relativePath, null, ProcessingStatus.Converted);
    }

    private static string NormalizeExtension(string ext) =>
        ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ? ".jpg" : ext.ToLowerInvariant();

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
