using EdsMediaArchiver.Helpers;
using FFMpegCore;
using FFMpegCore.Enums;

namespace EdsMediaArchiver.Services.Converters;

/// <summary>
/// Converts non-MP4 video formats to MP4.
/// </summary>
public class VideoConverter : IMediaConverter
{
    public bool IsSupported(string actualType) => MediaType.CompressibleImageTypes.Contains(actualType); // ToDo: What if already mp4?

    public async Task<string> ConvertAsync(string sourcePath, string outputDirectory, string actualType)
    {
        var outputPath = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(sourcePath) + ".mp4");
        outputPath = FileHelper.GetUniqueFilePath(outputPath);

        await FFMpegArguments
            .FromFileInput(sourcePath)
            .OutputToFile(outputPath, overwrite: false, options => options
                .WithVideoCodec("libx264")
                // CRF 17 for "Visually Lossless"
                .WithConstantRateFactor(17)
                .WithSpeedPreset(Speed.Slow)
                // High-Fidelity Audio
                // 128kbps is "Radio" quality. Use 256 or 320 for "Pro" audio.
                .WithAudioCodec("aac")
                .WithAudioBitrate(192)

                // Preserve Color Fidelity, "yuv420p" is the safe standard.
                .WithCustomArgument("-pix_fmt yuv420p")
                .WithCustomArgument("-map_metadata 0")
                .WithCustomArgument("-movflags use_metadata_tags"))
            .ProcessAsynchronously();

        return outputPath;
    }
}
