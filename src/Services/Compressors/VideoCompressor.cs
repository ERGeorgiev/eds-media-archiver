using EdsMediaArchiver.Definitions;
using EdsMediaArchiver.Helpers;
using FFMpegCore;
using FFMpegCore.Enums;

namespace EdsMediaArchiver.Services.Compressors;

public interface IVideoCompressor : IMediaCompressor { }

/// <summary>
/// Compresses video formats to MP4 (H.264 + AAC).
/// </summary>
public class VideoCompressor(IExifToolService exif) : IVideoCompressor
{
    public static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        MediaType.Asf, MediaType.Avi, MediaType.Amv, MediaType.Dv, MediaType.Dvr_ms, MediaType.F4V, MediaType.Flv, MediaType.Gxf, MediaType.Lrv,
        MediaType.M2Ts, MediaType.M4V, MediaType.Mj2, MediaType.Mjpeg, MediaType.Mkv, MediaType.Mod, MediaType.Mov, MediaType.Mp4, MediaType.Mpeg,
        MediaType.Mpegts, MediaType.Mpg, MediaType.Mts, MediaType.Mvi, MediaType.Mxf, MediaType.Ogv, MediaType.QuickTime, MediaType.Rm, MediaType.Rmvb, 
        MediaType.ThreeG2, MediaType.ThreeGp, MediaType.Tod, MediaType.Ts, MediaType.Vob, MediaType.Wmv, MediaType.Wtv
        // Any other types may be adversely affected by the current compressor, like WebM/AV1 that is
        // more efficient than the compressor's output MP4/H265.
        // In terms of conversion/standardization for DateWrite, it's less of an issue for videos (few/none support exif anyway)
    };

    public bool IsSupported(string actualType) => SupportedTypes.Contains(actualType);

    public async Task<string> CompressAsync(string sourcePath, string outputDirectory, string fileType, CompressorMode compressorMode)
    {
        var outputExtension = ".mp4";
        var sourceExtension = Path.GetExtension(sourcePath);
        var outputPath = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(sourcePath) + outputExtension);
        if (outputExtension.Equals(sourceExtension, StringComparison.OrdinalIgnoreCase))
        {
            if (compressorMode == CompressorMode.Convert)
            {
                return sourcePath;
            }
            else
            {
                // Prevent compression of already compressed files.
                var analysis = await FFProbe.AnalyseAsync(sourcePath);
                var videoStream = analysis.VideoStreams.FirstOrDefault();
                if (videoStream != null)
                {
                    bool isSmallEnough = videoStream.Width <= 1920 && videoStream.Height <= 1920;
                    bool isModernCodec = videoStream.CodecName == "h264" || videoStream.CodecName == "hevc";
                    double bitrateKbps = analysis.Format.BitRate / 1000.0;
                    bool isLowBitrate = bitrateKbps <= 10000;

                    if (isSmallEnough && isLowBitrate && isModernCodec)
                    {
                        return sourcePath; // Already compressed
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
                            .WithCustomArgument("-profile:v main")
                            .WithCustomArgument("-color_range 1") // Forces limited range
                            .WithCustomArgument("-movflags +faststart"); 
                        if (compressorMode == CompressorMode.CompressAndResize)
                        {
                            // If width/height is > 1920, scale width to 1920
                            options.WithCustomArgument("-vf \"scale=1920:1920:force_original_aspect_ratio=decrease:force_divisible_by=2\"");
                        }
                        else
                        {
                            // Ensure current dimensions are even (required for yuv420p)
                            options.WithCustomArgument("-vf \"scale='trunc(iw/2)*2:trunc(ih/2)*2'\"");
                        }
                    })
                    .ProcessAsynchronously();
                break;
            case CompressorMode.Convert:
                await FFMpegArguments
                    .FromFileInput(sourcePath)
                    .OutputToFile(outputPath, overwrite: false, options =>
                    {
                        options
                            .WithVideoCodec("copy")
                            .WithAudioCodec("copy")
                            .WithCustomArgument("-map_metadata 0")
                            .WithCustomArgument("-movflags +faststart");
                    })
                    .ProcessAsynchronously();
                break;
            default:
                throw new NotSupportedException($"Mode {compressorMode} not supported");
        }

        await exif.CopyMetadata(sourcePath, outputPath);

        return outputPath;
    }
}
