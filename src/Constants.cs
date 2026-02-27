using System.Text.RegularExpressions;
using TagLib;

namespace EdsMediaArchiver;

/// <summary>
/// File type classifications, extension mappings, and filename date patterns.
/// </summary>
public static partial class Constants
{
    public static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".mp4", ".mov", ".heic", ".heif",
        ".bmp", ".gif", ".webp", ".avi", ".mkv", ".wmv", ".3gp",
        ".tiff", ".tif"
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
        [MediaType.Wmv] = ".wmv",
        [MediaType.ThreeGp] = ".3gp",
        [MediaType.QuickTime] = ".mov"
    };

    /// <summary>Map from detected file type to correct extension (for fixing mislabeled files).</summary>
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
        [".wmv"] = MediaType.Wmv,
        [".3gp"] = MediaType.ThreeGp
    };

    /// <summary>
    /// Regex patterns for dates embedded in filenames.
    /// Matches YYYYMMDD with optional separators, optionally followed by HHmmss.
    /// Covers: 20231225, 2023-12-25, IMG_20231225_143022, etc.
    /// </summary>
    public static readonly Regex[] FilenamePatterns =
    {
        // YYYY[sep]MM[sep]DD[sep]HH[sep]mm[sep]ss (with time)
        DateTimePattern(),
        // YYYY[sep]MM[sep]DD (date only)
        DateOnlyPattern()
    };

    [GeneratedRegex(@"(?:^|[\s_\-\.\(~])(?<y>20\d{2})[_\-\.]?(?<m>[01]\d)[_\-\.]?(?<d>[0-3]\d)[_\-\.]?(?<H>[0-2]\d)[_\-\.]?(?<Min>[0-5]\d)[_\-\.]?(?<Sec>[0-5]\d)(?:$|[\s_\-\.\(\)~])", RegexOptions.Compiled)]
    private static partial Regex DateTimePattern();

    [GeneratedRegex(@"(?:^|[\s_\-\.\(~])(?<y>20\d{2})[_\-\.]?(?<m>[01]\d)[_\-\.]?(?<d>[0-3]\d)(?:$|[\s_\-\.\(\)~])", RegexOptions.Compiled)]
    private static partial Regex DateOnlyPattern();
}
