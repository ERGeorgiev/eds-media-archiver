using FFMpegCore;
using FFMpegCore.Enums;

namespace EdsMediaArchiver.Services.Compressors;

/// <summary>
/// Compresses non-MP4 video formats to MP4 (H.264 + AAC).
/// </summary>
public class VideoCompressor : IMediaCompressor
{
    public bool IsSupported(string actualType) => MediaType.CompressibleVideoTypes.Contains(actualType);

    public async Task<string?> CompressAsync(string sourcePath, string outputDirectory)
    {
        try
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
                    .WithAudioCodec("aac")
                    .WithAudioBitrate(128)
                    .WithCustomArgument("-map_metadata 0")
                    .WithCustomArgument("-movflags use_metadata_tags"))
                .ProcessAsynchronously();

            return File.Exists(outputPath) ? outputPath : null;
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
