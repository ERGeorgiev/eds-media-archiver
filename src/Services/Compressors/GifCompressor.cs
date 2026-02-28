using ImageMagick;

namespace EdsMediaArchiver.Services.Compressors;

/// <summary>
/// Handles GIF compression: single-frame GIFs become JPG, multi-frame GIFs become MP4.
/// </summary>
public class GifCompressor(IImageCompressor imageCompressor, IVideoCompressor videoCompressor) : IMediaCompressor
{
    public bool IsSupported(string actualType) =>
        actualType.Equals(MediaType.Gif, StringComparison.OrdinalIgnoreCase);

    public async Task<string> CompressAsync(string sourcePath, string outputDirectory)
    {
        var frameCount = GetFrameCount(sourcePath);

        if (frameCount <= 1)
            return await imageCompressor.CompressAsync(sourcePath, outputDirectory);
        else
            return await videoCompressor.CompressAsync(sourcePath, outputDirectory);
    }

    private static int GetFrameCount(string path)
    {
        using var images = new MagickImageCollection();
        images.Read(path);
        return images.Count;
    }
}
