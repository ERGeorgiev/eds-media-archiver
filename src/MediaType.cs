namespace EdsMediaArchiver;

public static class MediaType
{
    // Image Types
    public const string Jpeg = "JPEG";
    public const string Png = "PNG";
    public const string Heic = "HEIC";
    public const string Heif = "HEIF";
    public const string Webp = "WEBP";
    public const string Bmp = "BMP";
    public const string Gif = "GIF";
    public const string Tiff = "TIFF";

    // Video Types
    public const string Mp4 = "MP4";
    public const string Mov = "MOV";
    public const string QuickTime = "QuickTime";
    public const string Avi = "AVI";
    public const string Mkv = "MKV";
    public const string Wmv = "WMV";
    public const string ThreeGp = "3GP";
    public const string Unknown = "UNKNOWN";

    public static readonly HashSet<string> ExifWritableTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        Jpeg,
        Heic,
        Heif
    };

    public static readonly HashSet<string> VideoTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        Mp4,
        Mov,
        Avi,
        Mkv,
        Wmv,
        ThreeGp,
        QuickTime
    };

    /// <summary>File types that can only store XMP - need converting to JPG for proper EXIF.</summary>
    public static readonly HashSet<string> XmpOnlyTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        Webp,
        Bmp,
        Gif,
        Tiff
    };
}
