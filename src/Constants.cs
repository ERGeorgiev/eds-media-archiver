namespace EdsMediaArchiver;

/// <summary>
/// File type classifications, extension mappings, and filename date patterns.
/// </summary>
public static partial class Constants
{
    public static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Images
        ".jpg", ".jpeg", ".png", ".heic", ".heif",
        ".bmp", ".gif", ".webp", ".tiff", ".tif",
        // Video
        ".mp4", ".mov", ".avi", ".mkv", ".mts", ".wmv", ".3gp",
        // Audio
        ".mp3", ".wav", ".flac", ".ogg", ".aac", ".wma", ".m4a"
    };

    /// <summary>Map from detected file type to correct extension (for fixing mislabeled files).</summary>
    public static readonly Dictionary<string, string> FileTypeToExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [MediaType.Jpeg] = ".jpg",
        [MediaType.Png] = ".png",
        [MediaType.Heic] = ".heic",
        [MediaType.Heif] = ".heif",
        [MediaType.Webp] = ".webp",
        [MediaType.Bmp] = ".bmp",
        [MediaType.Gif] = ".gif",
        [MediaType.Tiff] = ".tiff",
        [MediaType.Mp4] = ".mp4",
        [MediaType.Mov] = ".mov",
        [MediaType.Avi] = ".avi",
        [MediaType.Mkv] = ".mkv",
        [MediaType.Mts] = ".mts",
        [MediaType.Wmv] = ".wmv",
        [MediaType.ThreeGp] = ".3gp",
        [MediaType.QuickTime] = ".mov",
        [MediaType.Mp3] = ".mp3",
        [MediaType.Wav] = ".wav",
        [MediaType.Flac] = ".flac",
        [MediaType.Ogg] = ".ogg",
        [MediaType.Aac] = ".aac",
        [MediaType.Wma] = ".wma",
        [MediaType.M4a] = ".m4a"
    };

    /// <summary>Map from extension to file type for fallback detection.</summary>
    public static readonly Dictionary<string, string> ExtensionToFileType = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = MediaType.Jpeg,
        [".jpeg"] = MediaType.Jpeg,
        [".png"] = MediaType.Png,
        [".heic"] = MediaType.Heic,
        [".heif"] = MediaType.Heif,
        [".webp"] = MediaType.Webp,
        [".bmp"] = MediaType.Bmp,
        [".gif"] = MediaType.Gif,
        [".tiff"] = MediaType.Tiff,
        [".tif"] = MediaType.Tiff,
        [".mp4"] = MediaType.Mp4,
        [".mov"] = MediaType.Mov,
        [".avi"] = MediaType.Avi,
        [".mkv"] = MediaType.Mkv,
        [".mts"] = MediaType.Mts,
        [".wmv"] = MediaType.Wmv,
        [".3gp"] = MediaType.ThreeGp,
        [".mp3"] = MediaType.Mp3,
        [".wav"] = MediaType.Wav,
        [".flac"] = MediaType.Flac,
        [".ogg"] = MediaType.Ogg,
        [".aac"] = MediaType.Aac,
        [".wma"] = MediaType.Wma,
        [".m4a"] = MediaType.M4a
    };
}
