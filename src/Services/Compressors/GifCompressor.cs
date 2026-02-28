using FFMpegCore;
using FFMpegCore.Enums;
using ImageMagick;

namespace EdsMediaArchiver.Services.Compressors;

/// <summary>
/// Handles GIF compression: single-frame GIFs become JPG, multi-frame GIFs become MP4.
/// </summary>
public class GifCompressor : IMediaCompressor
{
    public bool IsSupported(string actualType) =>
        actualType.Equals(MediaType.Gif, StringComparison.OrdinalIgnoreCase);

    public async Task<string?> CompressAsync(string sourcePath, string outputDirectory)
    {
        try
        {
            var frameCount = GetFrameCount(sourcePath);

            if (frameCount <= 1)
                return await ConvertToJpgAsync(sourcePath, outputDirectory);
            else
                return await ConvertToMp4Async(sourcePath, outputDirectory);
        }
        catch
        {
            return null;
        }
    }

    private static int GetFrameCount(string path)
    {
        using var images = new MagickImageCollection();
        images.Read(path);
        return images.Count;
    }

    private static async Task<string?> ConvertToJpgAsync(string sourcePath, string outputDirectory)
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

    private static async Task<string?> ConvertToMp4Async(string sourcePath, string outputDirectory)
    {
        var outputPath = Path.Combine(outputDirectory,
            Path.GetFileNameWithoutExtension(sourcePath) + ".mp4");
        outputPath = GetUniqueFilePath(outputPath);

        await FFMpegArguments
            .FromFileInput(sourcePath)
            .OutputToFile(outputPath, overwrite: false, options => options
                .WithVideoCodec("libx264")
                .WithConstantRateFactor(23)
                .WithSpeedPreset(Speed.Slow)
                .WithCustomArgument("-pix_fmt yuv420p")
                .WithCustomArgument("-movflags +faststart"))
            .ProcessAsynchronously();

        return File.Exists(outputPath) ? outputPath : null;
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
