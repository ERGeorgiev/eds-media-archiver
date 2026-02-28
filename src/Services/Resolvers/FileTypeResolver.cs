using MetadataExtractor.Util;

namespace EdsMediaArchiver.Services.Resolvers;

public interface IFileTypeResolver
{
    string GetActualFileType(string filePath);
}

public class FileTypeResolver : IFileTypeResolver
{
    /// <summary>
    /// Detects the actual file type using magic bytes via MetadataExtractor,
    /// returning an uppercase type string matching the convention used by <see cref="MediaType"/>.
    /// Falls back to extension-based inference for types MetadataExtractor cannot detect.
    /// </summary>
    public string GetActualFileType(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            var fileType = FileTypeDetector.DetectFileType(stream);

            // MetadataExtractor reports all HEIF-family files as Heif;
            // preserve HEIC vs HEIF distinction using the file extension.
            if (fileType == FileType.Heif)
            {
                var ext = Path.GetExtension(filePath);
                return ext.Equals(".heic", StringComparison.OrdinalIgnoreCase) ? "HEIC" : "HEIF";
            }

            if (fileType != FileType.Unknown)
                return NormalizeFileType(fileType);
        }
        catch { }

        return InferTypeFromExtension(filePath);
    }

    private static string NormalizeFileType(FileType fileType)
    {
        return fileType switch
        {
            FileType.Jpeg => MediaType.Jpeg,
            FileType.Png => MediaType.Png,
            FileType.Gif => MediaType.Gif,
            FileType.Bmp => MediaType.Bmp,
            FileType.Tiff => MediaType.Tiff,
            FileType.WebP => MediaType.Webp,
            FileType.Heif => MediaType.Heif,
            FileType.QuickTime => MediaType.QuickTime,
            FileType.Mp4 => MediaType.Mp4,
            FileType.Avi => MediaType.Avi,
            _ => fileType.ToString().ToUpperInvariant()
        };
    }

    private static string InferTypeFromExtension(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return Constants.ExtensionToFileType.TryGetValue(ext, out var fileType) ? fileType : MediaType.Unknown;
    }
}
