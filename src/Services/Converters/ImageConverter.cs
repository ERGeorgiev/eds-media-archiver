using EdsMediaArchiver.Helpers;
using EdsMediaArchiver.Services.Compressors;
using ImageMagick;

namespace EdsMediaArchiver.Services.Converters;

public interface IImageConverter : IMediaConverter { }

/// <summary>
/// Converts XMP-only image formats (WebP, BMP, TIFF) to JPG.
/// </summary>
public class ImageConverter : IImageConverter
{
    public bool IsSupported(string actualType) => MediaType.CompressibleImageTypes.Contains(actualType);

    public async Task<string> ConvertAsync(string sourcePath, string outputDirectory, string actualType)
    {
        var outputPath = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(sourcePath) + ".jpg");
        if (sourcePath == outputPath)
            return outputPath; // Already processed
        outputPath = FileHelper.GetUniqueFilePath(outputPath);

        using var image = new MagickImage();
        await image.ReadAsync(sourcePath);

        // 1. Conversion = Max Quality
        image.Quality = 92;

        // 2. Disable Chroma Subsampling (Full Color Detail 4:4:4)
        image.Settings.SetDefine("jpeg:sampling-factor", "1x1,1x1,1x1");

        // 3. COLOR FIDELITY: Ensure the ICC profile is present. Keep image profile (like Adobe RGB). 
        // If no profile, define sRGB so browsers don't guess.
        var profile = image.GetColorProfile();
        if (profile == null)
        {
            image.ColorSpace = ColorSpace.sRGB;
        }

        await image.WriteAsync(outputPath, MagickFormat.Jpeg);
        return outputPath;
    }
}
