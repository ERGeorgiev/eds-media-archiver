using System.Text.RegularExpressions;

namespace PersonalMediaArchiver;

/// <summary>
/// File type classifications, extension mappings, and filename date patterns
/// ported from the PowerShell Fix-MediaDates script.
/// </summary>
public static class Constants
{
    public static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".mp4", ".mov", ".heic", ".heif",
        ".bmp", ".gif", ".webp", ".avi", ".mkv", ".wmv", ".3gp",
        ".tiff", ".tif"
    };

    public static readonly HashSet<string> ExifWritableTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "JPEG", "HEIC", "HEIF"
    };

    public static readonly HashSet<string> VideoTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "MP4", "MOV", "AVI", "MKV", "WMV", "3GP", "QuickTime"
    };

    /// <summary>File types that can only store XMP - need converting to JPG for proper EXIF.</summary>
    public static readonly HashSet<string> XmpOnlyTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "WEBP", "BMP", "GIF", "TIFF"
    };

    /// <summary>Map from detected file type to correct extension (for fixing mislabeled files).</summary>
    public static readonly Dictionary<string, string> TypeToExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        ["JPEG"] = ".jpg",  ["PNG"]  = ".png",  ["HEIC"] = ".heic", ["HEIF"] = ".heif",
        ["WEBP"] = ".webp", ["BMP"]  = ".bmp",  ["GIF"]  = ".gif",  ["TIFF"] = ".tiff",
        ["MP4"]  = ".mp4",  ["MOV"]  = ".mov",  ["AVI"]  = ".avi",  ["MKV"]  = ".mkv",
        ["WMV"]  = ".wmv",  ["3GP"]  = ".3gp",  ["QuickTime"] = ".mov"
    };

    /// <summary>
    /// Regex patterns for dates embedded in filenames.
    /// Matches YYYYMMDD with optional separators, optionally followed by HHmmss.
    /// Covers: 20231225, 2023-12-25, IMG_20231225_143022, etc.
    /// </summary>
    public static readonly Regex[] FilenamePatterns =
    {
        // YYYY[sep]MM[sep]DD[sep]HH[sep]mm[sep]ss (with time)
        new(@"(?:^|[\s_\-\.\(~])(?<y>20\d{2})[_\-\.]?(?<m>[01]\d)[_\-\.]?(?<d>[0-3]\d)[_\-\.]?(?<H>[0-2]\d)[_\-\.]?(?<Min>[0-5]\d)[_\-\.]?(?<Sec>[0-5]\d)(?:$|[\s_\-\.\(\)~])", RegexOptions.Compiled),
        // YYYY[sep]MM[sep]DD (date only)
        new(@"(?:^|[\s_\-\.\(~])(?<y>20\d{2})[_\-\.]?(?<m>[01]\d)[_\-\.]?(?<d>[0-3]\d)(?:$|[\s_\-\.\(\)~])", RegexOptions.Compiled)
    };
}
