using EdsMediaArchiver.Definitions;
using EdsMediaArchiver.Helpers;
using ImageMagick;

namespace EdsMediaArchiver.Services.Compressors;

public interface IImageCompressor : IMediaCompressor { }

/// <summary>
/// Compresses XMP-only image formats (WebP, BMP, TIFF) to JPG.
/// </summary>
public class ImageCompressor : IImageCompressor
{
    public static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        MediaType.Jpeg, MediaType.Png, MediaType.Bmp
        // Any other types may be adversely affected by the current compressor, like HEIC/HEIF that are
        // more efficient than the compressor's output JPEG.
    };

    public bool IsSupported(string actualType) => SupportedTypes.Contains(actualType);

    public async Task<string> CompressAsync(string sourcePath, string outputDirectory, CompressorMode compressorMode)
    {
        var outputPath = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(sourcePath) + ".jpg");
        if (sourcePath == outputPath && compressorMode == CompressorMode.Convert) // source already .jpg
        {
            return outputPath;
        }

        using var image = new MagickImage();
        await image.ReadAsync(sourcePath);
        if (sourcePath == outputPath && compressorMode != CompressorMode.Convert)
        {
            // CRITERIA CHECK: If already .jpg, check if it actually needs Compression
            bool isTooLarge = image.Width > 1920 || image.Height > 1920;
            bool isHighQuality = image.Quality > 88;

            if (!isTooLarge && !isHighQuality)
            {
                // If it's already small and lower quality, do not compress to avoid generation loss.
                return outputPath;
            }
        }

        image.AutoOrient();
        switch (compressorMode)
        {
            case CompressorMode.CompressAndResize:
            case CompressorMode.Compress:
                image.Quality = 85;
                image.Settings.SetDefine("jpeg:sampling-factor", "4:2:0");
                image.Settings.Interlace = Interlace.Plane;
                image.ColorSpace = ColorSpace.sRGB; 
                if (compressorMode == CompressorMode.CompressAndResize)
                {
                    var size = new MagickGeometry("1920x1920>");
                    image.Resize(size);
                }
                break;
            case CompressorMode.Convert:
                image.Quality = 92;
                image.Settings.SetDefine("jpeg:sampling-factor", "1x1,1x1,1x1");
                // COLOR FIDELITY: Ensure the ICC profile is present. Keep image profile (like Adobe RGB). 
                // If no profile, define sRGB so browsers don't guess.
                if (image.GetColorProfile() == null)
                {
                    image.ColorSpace = ColorSpace.sRGB;
                }
                break;
            default:
                throw new NotSupportedException($"Mode {compressorMode} not supported");
        }

        outputPath = FileHelper.GetUniqueFilePath(outputPath);
        await image.WriteAsync(outputPath, MagickFormat.Jpeg);
        return outputPath;
    }
}
