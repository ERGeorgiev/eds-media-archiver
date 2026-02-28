using EdsMediaArchiver.Helpers;
using ImageMagick;

namespace EdsMediaArchiver.Services.Compressors;

public interface IImageCompressor : IMediaCompressor { }

/// <summary>
/// Compresses XMP-only image formats (WebP, BMP, TIFF) to JPG.
/// </summary>
public class ImageCompressor : IImageCompressor
{
    public bool IsSupported(string actualType) => MediaType.CompressibleImageTypes.Contains(actualType);

    public async Task<string> CompressAsync(string sourcePath, string outputDirectory)
    {
        var outputPath = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(sourcePath) + ".jpg");
        outputPath = FileHelper.GetUniqueFilePath(outputPath);

        using var image = new MagickImage();
        await image.ReadAsync(sourcePath);

        image.Quality = 95;
        image.Settings.SetDefine("jpeg:sampling-factor", "4:2:0");
        image.ColorSpace = ColorSpace.sRGB;

        await image.WriteAsync(outputPath, MagickFormat.Jpeg);
        return outputPath;
    }
}
