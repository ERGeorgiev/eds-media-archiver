using ImageMagick;

namespace EdsMediaArchiver.Services.Compressors;

/// <summary>
/// Compresses XMP-only image formats (WebP, BMP, TIFF) to JPG.
/// </summary>
public class ImageCompressor : IMediaCompressor
{
    public bool IsSupported(string actualType) => MediaType.CompressibleImageTypes.Contains(actualType);

    public async Task<string?> CompressAsync(string sourcePath, string outputDirectory)
    {
        try
        {
            var outputPath = Path.Combine(outputDirectory,
                Path.GetFileNameWithoutExtension(sourcePath) + ".jpg");
            outputPath = GetUniqueFilePath(outputPath);

            using var image = new MagickImage();
            await image.ReadAsync(sourcePath);

            image.Quality = 95;
            image.Settings.SetDefine("jpeg:sampling-factor", "4:2:0");
            image.ColorSpace = ColorSpace.sRGB;

            await image.WriteAsync(outputPath, MagickFormat.Jpeg);
            return outputPath;
        }
        catch
        {
            return null;
        }
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
