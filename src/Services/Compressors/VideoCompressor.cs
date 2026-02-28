using EdsMediaArchiver.Helpers;
using FFMpegCore;
using FFMpegCore.Enums;

namespace EdsMediaArchiver.Services.Compressors;

public interface IVideoCompressor : IMediaCompressor { }

/// <summary>
/// Compresses non-MP4 video formats to MP4 (H.264 + AAC).
/// </summary>
public class VideoCompressor : IVideoCompressor
{
    public bool IsSupported(string actualType) => MediaType.SupportedVideoTypes.Contains(actualType);

    public async Task<string> CompressAsync(string sourcePath, string outputDirectory)
    {
        var outputPath = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(sourcePath) + ".mp4");
        outputPath = FileHelper.GetUniqueFilePath(outputPath);

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

        return outputPath;
    }
}
