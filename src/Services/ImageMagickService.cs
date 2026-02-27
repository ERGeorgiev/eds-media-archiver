using ImageMagick;

namespace PersonalMediaArchiver.Services;

/// <summary>
/// Uses Magick.NET for image conversion and file type detection.
/// Only required for converting XMP-only formats (WebP, BMP, GIF, TIFF) to JPG.
/// </summary>
public class ImageMagickService
{
    /// <summary>
    /// Converts an image to JPG with quality and colorspace settings matching
    /// the original CLI invocation: -quality 95 -sampling-factor 4:2:0 -colorspace sRGB.
    /// </summary>
    public async Task<bool> ConvertToJpgAsync(string sourcePath, string destPath)
    {
        try
        {
            using var image = new MagickImage();
            await image.ReadAsync(sourcePath);

            image.Quality = 95;
            image.Settings.SetDefine("jpeg:sampling-factor", "4:2:0");
            image.ColorSpace = ColorSpace.sRGB;

            await image.WriteAsync(destPath, MagickFormat.Jpeg);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Detects the actual file type using Magick.NET's format detection (magic bytes),
    /// returning an uppercase type string matching ExifTool conventions.
    /// </summary>
    public string DetectFileType(string filePath)
    {
        try
        {
            var info = new MagickImageInfo(filePath);
            return NormalizeFormat(info.Format);
        }
        catch
        {
            return "UNKNOWN";
        }
    }

    private static string NormalizeFormat(MagickFormat format)
    {
        return format switch
        {
            MagickFormat.Jpeg or MagickFormat.Jpg => "JPEG",
            MagickFormat.Png or MagickFormat.Png00 or MagickFormat.Png8
                or MagickFormat.Png24 or MagickFormat.Png32 or MagickFormat.Png48
                or MagickFormat.Png64 => "PNG",
            MagickFormat.Heic => "HEIC",
            MagickFormat.Heif => "HEIF",
            MagickFormat.WebP => "WEBP",
            MagickFormat.Bmp or MagickFormat.Bmp2 or MagickFormat.Bmp3 => "BMP",
            MagickFormat.Gif or MagickFormat.Gif87 => "GIF",
            MagickFormat.Tiff or MagickFormat.Tiff64 => "TIFF",
            _ => format.ToString().ToUpperInvariant()
        };
    }
}
