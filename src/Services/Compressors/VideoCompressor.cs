using EdsMediaArchiver.Definitions;
using EdsMediaArchiver.Helpers;
using FFMpegCore;
using FFMpegCore.Enums;

namespace EdsMediaArchiver.Services.Compressors;

public interface IVideoCompressor : IMediaCompressor { }

/// <summary>
/// Compresses video formats to MP4 (H.264 + AAC).
/// </summary>
public class VideoCompressor : IVideoCompressor
{
    public static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        MediaType.Asf, MediaType.Avi, MediaType.Amv, MediaType.Dv, MediaType.Dvr_ms, MediaType.F4V, MediaType.Flv, MediaType.Gxf, MediaType.Lrv,
        MediaType.M2Ts, MediaType.M4V, MediaType.Mj2, MediaType.Mjpeg, MediaType.Mkv, MediaType.Mod, MediaType.Mov, MediaType.Mp4, MediaType.Mpeg,
        MediaType.Mpegts, MediaType.Mpg, MediaType.Mts, MediaType.Mvi, MediaType.Mxf, MediaType.Ogv, MediaType.Rm, MediaType.Rmvb, MediaType.ThreeG2,
        MediaType.ThreeGp, MediaType.Tod, MediaType.Ts, MediaType.Vob, MediaType.Wmv, MediaType.Wtv,
        // Any other types may be adversely affected by the current compressor, like WebM/AV1 that is
        // more efficient than the compressor's output MP4/H265.
        // In terms of conversion/standardization for DateWrite, it's less of an issue for videos (few/none support exif anyway)
    };

    public bool IsSupported(string actualType) => SupportedTypes.Contains(actualType);

    public async Task<string> CompressAsync(string sourcePath, string outputDirectory, CompressorMode compressorMode)
    {
        var outputPath = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(sourcePath) + ".mp4");
        if (sourcePath == outputPath)
        {
            if (compressorMode == CompressorMode.Convert)
            {
                return outputPath; // Already converted
            }
            else
            {
                // Prevent compression of already compressed files.
                var analysis = await FFProbe.AnalyseAsync(sourcePath);
                var videoStream = analysis.VideoStreams.FirstOrDefault();
                if (videoStream != null)
                {
                    bool isSmallEnough = videoStream.Width <= 1920 && videoStream.Height <= 1920;

                    double bitrateKbps = analysis.Format.BitRate / 1000.0;
                    bool isLowBitrate = bitrateKbps <= 5000;

                    bool isModernCodec = videoStream.CodecName == "h264" || videoStream.CodecName == "hevc";


                    if (isSmallEnough && isLowBitrate && isModernCodec)
                    {
                        return outputPath; // Already compressed
                    }
                }
            }
        }

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
                            .WithVideoCodec("libx264")
                            .WithConstantRateFactor(23)
                            .WithSpeedPreset(Speed.Slow)
                            .WithAudioCodec("aac")
                            .WithAudioBitrate(128)
                            .WithCustomArgument("-pix_fmt yuv420p")
                            .WithCustomArgument("-map_metadata 0")
                            .WithCustomArgument("-movflags use_metadata_tags");
                        if (compressorMode == CompressorMode.CompressAndResize)
                        {
                            // If input width (iw) is > 1920, scale width to 1920 and height proportionally (-2).
                            // The '-2' ensures the resulting height is always an even number (required for x264).
                            options.WithCustomArgument("-vf \"scale='min(1920,iw)':-2\"");
                        }
                    })
                    .ProcessAsynchronously();
                break;
            case CompressorMode.Convert:
                await FFMpegArguments
                    .FromFileInput(sourcePath)
                    .OutputToFile(outputPath, overwrite: false, options => options
                        .WithVideoCodec("libx264")
                        // CRF 17 for "Visually Lossless"
                        .WithConstantRateFactor(17)
                        .WithSpeedPreset(Speed.Slow)
                        // High-Fidelity Audio
                        .WithAudioCodec("aac")
                        .WithAudioBitrate(192)
                        // Preserve Color Fidelity, "yuv420p" is the safe standard.
                        .WithCustomArgument("-pix_fmt yuv420p")
                        .WithCustomArgument("-map_metadata 0")
                        .WithCustomArgument("-movflags use_metadata_tags"))
                    .ProcessAsynchronously();
                break;
            default:
                throw new NotSupportedException($"Mode {compressorMode} not supported");
        }

        return outputPath;
    }
}
