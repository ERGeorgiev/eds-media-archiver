using EdsMediaArchiver.Definitions;
using EdsMediaArchiver.Helpers;
using FFMpegCore;

namespace EdsMediaArchiver.Services.Compressors;

/// <summary>
/// Compresses audio files to OGG (Vorbis).
/// </summary>
public class AudioCompressor : IMediaCompressor
{
    public static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        MediaType.Mp3, MediaType.Wav, MediaType.Flac, MediaType.Ogg, MediaType.Aac, MediaType.Wma, MediaType.M4a, MediaType.Opus,
        MediaType.Aiff, MediaType.Amr, MediaType.Ac3, MediaType.Dts, MediaType.Pcm, MediaType.M4b, MediaType.Mp2
        // As this compressor uses Opus, it's an upgrade for pretty much all other audio extensions listed here.
    };

    public bool IsSupported(string actualType) => SupportedTypes.Contains(actualType);

    public bool OutputMp4InsteadOfOgg { get; set; } = false;
    public bool ConvertOnly { get; set; } = false;

    public async Task<string> CompressAsync(string sourcePath, string outputDirectory, string fileType, CompressorMode compressorMode)
    {
        var outputExtension = ".ogg";
        var sourceExtension = Path.GetExtension(sourcePath);
        var outputPath = Path.Combine(outputDirectory,
            Path.GetFileNameWithoutExtension(sourcePath) + outputExtension);
        if (outputExtension == sourceExtension)
            return sourcePath; // Already processed, other .ogg are likely small enough already.

        outputPath = FileHelper.GetUniqueFilePath(outputPath);
        switch (compressorMode)
        {
            case CompressorMode.CompressAndResize:
            case CompressorMode.Compress:
                await FFMpegArguments
                    .FromFileInput(sourcePath)
                    .OutputToFile(outputPath, overwrite: false, options =>
                    {
                        options
                        .WithAudioCodec("libopus")
                        .WithAudioBitrate(128)
                        .WithCustomArgument("-frame_size 60")
                        .WithCustomArgument("-vbr on")
                        .WithCustomArgument("-vn")
                        .WithCustomArgument("-map_metadata 0");
                    })
                    .ProcessAsynchronously();
                break;
            case CompressorMode.Convert:
                await FFMpegArguments
                    .FromFileInput(sourcePath)
                    .OutputToFile(outputPath, overwrite: false, options =>
                    {
                        options
                        .WithAudioCodec("copy")
                        .WithCustomArgument("-map_metadata 0"); // preserve all metadata
                    })
                    .ProcessAsynchronously();
                break;
            default:
                throw new NotSupportedException($"Mode {compressorMode} not supported");
        }

        return outputPath;
    }
}
