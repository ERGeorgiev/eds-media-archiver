using EdsMediaArchiver.Helpers;
using FFMpegCore;

namespace EdsMediaArchiver.Services.Compressors;

/// <summary>
/// Compresses audio files to OGG (Vorbis).
/// </summary>
public class AudioCompressor : IMediaCompressor
{
    public bool IsSupported(string actualType) =>
        MediaType.AudioTypes.Contains(actualType) &&
        !actualType.Equals(MediaType.Ogg, StringComparison.OrdinalIgnoreCase);

    public async Task<string> CompressAsync(string sourcePath, string outputDirectory)
    {
        var outputPath = Path.Combine(outputDirectory,
            Path.GetFileNameWithoutExtension(sourcePath) + ".ogg");
        outputPath = FileHelper.GetUniqueFilePath(outputPath);

        await FFMpegArguments
            .FromFileInput(sourcePath)
            .OutputToFile(outputPath, overwrite: false, options => options
                .WithAudioCodec("libvorbis")
                .WithCustomArgument("-qscale:a 5")
                .WithCustomArgument("-vn"))
            .ProcessAsynchronously();

        return outputPath;
    }
}
