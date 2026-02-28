using EdsMediaArchiver.Models;

namespace EdsMediaArchiver.Services.Processors;

public interface IExtensionFixProcessor
{
    /// <summary>
    /// Fixes the file extension if it doesn't match the actual detected type.
    /// Updates request.NewPath if renamed. Returns true if the file was renamed.
    /// </summary>
    bool Process(ArchiveRequest request, string actualType);
}

public class ExtensionFixProcessor : IExtensionFixProcessor
{
    public bool Process(ArchiveRequest request, string actualType)
    {
        if (!Constants.FileTypeToExtension.TryGetValue(actualType, out var correctExt))
            return false;

        var currentExt = Path.GetExtension(request.NewPath.Absolute);
        var normCurrent = NormalizeExtension(currentExt);
        var normCorrect = NormalizeExtension(correctExt);

        if (normCurrent.Equals(normCorrect, StringComparison.OrdinalIgnoreCase))
            return false;

        var newPath = Path.ChangeExtension(request.NewPath.Absolute, correctExt);
        newPath = GetUniqueFilePath(newPath);
        request.NewPath = new(request.NewPath.Root, newPath);

        File.Move(request.NewPath.Absolute, request.NewPath.Absolute);

        Console.WriteLine($"  [RENAME] {request.NewPath.Relative} -> {Path.GetFileName(newPath)} (actual type: {actualType})");
        return true;
    }

    private static string NormalizeExtension(string ext)
    {
        return ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ? ".jpg" : ext.ToLowerInvariant();
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
