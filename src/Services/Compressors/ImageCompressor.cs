using EdsMediaArchiver.Definitions;
using EdsMediaArchiver.Helpers;
using EdsMediaArchiver.Services.Resolvers;
using ImageMagick;

namespace EdsMediaArchiver.Services.Compressors;

public interface IImageCompressor : IMediaCompressor { }

/// <summary>
/// Compresses XMP-only image formats (WebP, BMP, TIFF) to JPG.
/// </summary>
public class ImageCompressor(IFileDateResolver fileDateResolver) : IImageCompressor
{
    public static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        MediaType.Jpeg, MediaType.Png, MediaType.Bmp
        // Any other types may be adversely affected by the current compressor, like HEIC/HEIF that are
        // more efficient than the compressor's output JPEG.
    };

    public bool IsSupported(string actualType) => SupportedTypes.Contains(actualType);

    public async Task<string> CompressAsync(string sourcePath, string outputDirectory, string fileType, CompressorMode compressorMode)
    {
        var outputExtension = ".jpg";
        var sourceExtension = Path.GetExtension(sourcePath);
        var outputPath = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(sourcePath) + outputExtension);
        if (outputExtension == sourceExtension && compressorMode == CompressorMode.Convert)
        {
            return sourceExtension; // source already .jpg
        }

        using var image = new MagickImage();
        await image.ReadAsync(sourcePath);
        if (outputExtension == sourceExtension && compressorMode != CompressorMode.Convert)
        {
            // CRITERIA CHECK: If already .jpg, check if it actually needs Compression
            bool isTooLarge = image.Width > 1920 || image.Height > 1920;
            bool isHighQuality = image.Quality > 88;

            if (!isTooLarge && !isHighQuality)
            {
                // If it's already small and lower quality, do not compress to avoid generation loss.
                return sourcePath;
            }
        }

        DateTimeOffset? setDate = fileDateResolver.ResolveBestDate(fileType, sourcePath);
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

        if (setDate.HasValue)
        {
            var exifDate = setDate.Value.LocalDateTime.ToString("yyyy:MM:dd HH:mm:ss");
            string exifOffset = setDate.Value.ToString("zzz");
            var profile = image.GetExifProfile() ?? new ExifProfile();

            profile.SetValue(ExifTag.DateTimeOriginal, exifDate);
            profile.SetValue(ExifTag.DateTimeDigitized, exifDate);
            profile.SetValue(ExifTag.DateTime, exifDate);
            profile.SetValue(ExifTag.OffsetTimeOriginal, exifOffset);
            profile.SetValue(ExifTag.OffsetTimeDigitized, exifOffset);
            profile.SetValue(ExifTag.OffsetTime, exifOffset);

            image.SetProfile(profile);

            // OPTIONAL: Update IPTC or XMP if your archiver needs to be super thorough
            image.SetAttribute("exif:DateTimeOriginal", exifDate);
        }

        outputPath = FileHelper.GetUniqueFilePath(outputPath);
        await image.WriteAsync(outputPath, MagickFormat.Jpeg);
        if (setDate.HasValue)
        {
            File.SetCreationTime(outputPath, setDate.Value.LocalDateTime);
            File.SetLastWriteTime(outputPath, setDate.Value.LocalDateTime);
        }

        return outputPath;
    }
}
