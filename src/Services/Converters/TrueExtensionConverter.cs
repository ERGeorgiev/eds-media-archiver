using EdsMediaArchiver.Helpers;
using TagLib;

namespace EdsMediaArchiver.Services.Converters;

/// <summary>
/// Renames files to ensure their extension matches their actual type.
/// </summary>
public class TrueExtensionConverter : IMediaConverter
{
    public bool IsSupported(string actualType) => Constants.FileTypeToExtension.ContainsKey(actualType);

    public Task<string> ConvertAsync(string sourcePath, string outputDirectory, string actualType)
    {
        if (Constants.FileTypeToExtension.TryGetValue(actualType, out var correctExt) == false)
            throw new UnsupportedFormatException($"File '{sourcePath}' with type '{actualType}' is not supported.");
        if (Constants.ExtensionToFileType.TryGetValue(correctExt, out var correctExtFileType) == false)
            throw new UnsupportedFormatException($"File '{sourcePath}' with type '{actualType}' is not supported.");
        var currentExt = Path.GetExtension(sourcePath);
        if (Constants.ExtensionToFileType.TryGetValue(currentExt, out var currentExtFileType) == false)
            throw new UnsupportedFormatException($"File '{sourcePath}' with type '{actualType}' is not supported.");

        if (currentExtFileType.Equals(correctExtFileType, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(sourcePath);

        var oldPath = sourcePath;
        var newPath = Path.ChangeExtension(oldPath, correctExt);
        newPath = FileHelper.GetUniqueFilePath(newPath);

        System.IO.File.Move(oldPath, newPath);

        Console.WriteLine($"  [RENAME] {oldPath} -> {Path.GetFileName(newPath)} (actual type: {actualType})");
        return Task.FromResult(newPath);
    }
}
