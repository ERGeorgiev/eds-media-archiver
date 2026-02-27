namespace PersonalMediaArchiver.Services;

/// <summary>
/// Wrapper around ImageMagick's "magick" CLI for image conversion.
/// Only required for converting XMP-only formats (WebP, BMP, GIF, TIFF) to JPG.
/// </summary>
public class ImageMagickService
{
    public static bool IsAvailable() => ProcessRunner.IsAvailable("magick", "-version");

    public async Task<bool> ConvertToJpgAsync(string sourcePath, string destPath)
    {
        var (_, exitCode) = await ProcessRunner.RunAsync(
            "magick", sourcePath,
            "-quality", "95",
            "-sampling-factor", "4:2:0",
            "-colorspace", "sRGB",
            destPath);
        return exitCode == 0;
    }
}
