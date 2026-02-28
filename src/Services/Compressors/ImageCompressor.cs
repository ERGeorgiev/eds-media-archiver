using EdsMediaArchiver.Helpers;
using ImageMagick;

namespace EdsMediaArchiver.Services.Compressors;

public interface IImageCompressor : IMediaCompressor { }

/// <summary>
/// Compresses XMP-only image formats (WebP, BMP, TIFF) to JPG.
/// </summary>
public class ImageCompressor : IImageCompressor
{
    public bool IsSupported(string actualType) => MediaType.SupportedImageTypes.Contains(actualType);

    public async Task<string> CompressAsync(string sourcePath, string outputDirectory)
    {
        var outputPath = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(sourcePath) + ".jpg");
        outputPath = FileHelper.GetUniqueFilePath(outputPath);

        using var image = new MagickImage();
        await image.ReadAsync(sourcePath);

        image.AutoOrient();
        // The '>' flag means "only shrink if the image is larger than the dimensions"
        var size = new MagickGeometry($"1920x1920>");
        image.Quality = 85;
        image.Settings.SetDefine("jpeg:sampling-factor", "4:2:0"); 
        image.Settings.Interlace = Interlace.Plane;
        image.ColorSpace = ColorSpace.sRGB;

        await image.WriteAsync(outputPath, MagickFormat.Jpeg);
        return outputPath;
    }
}
