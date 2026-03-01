using EdsMediaArchiver.Definitions;
using ImageMagick;

namespace EdsMediaArchiver.Services.Compressors;

/// <summary>
/// Handles mixed-format compression: single-frame GIFs is treated as image, multi-frame GIFs is treated as video.
/// </summary>
public class MixedFormatCompressor(IImageCompressor imageCompressor, IVideoCompressor videoCompressor) : IMediaCompressor
{
    public static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        MediaType.Gif, MediaType.Apng, MediaType.Webp
    };

    public bool IsSupported(string actualType) => SupportedTypes.Contains(actualType);

    public async Task<string> CompressAsync(string sourcePath, string outputDirectory, string fileType, CompressorMode compressorMode)
    {
        var frameCount = GetFrameCount(sourcePath);

        if (frameCount <= 1)
            return await imageCompressor.CompressAsync(sourcePath, outputDirectory, fileType, compressorMode);
        else
            return await videoCompressor.CompressAsync(sourcePath, outputDirectory, fileType, compressorMode);
    }

    private static int GetFrameCount(string path)
    {
        using var images = new MagickImageCollection();
        images.Read(path);
        return images.Count;
    }
}
