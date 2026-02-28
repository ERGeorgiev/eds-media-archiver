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

    public async Task<string?> CompressAsync(string sourcePath, string outputDirectory)
    {
        try
        {
            var outputPath = Path.Combine(outputDirectory,
                Path.GetFileNameWithoutExtension(sourcePath) + ".ogg");
            outputPath = GetUniqueFilePath(outputPath);

            await FFMpegArguments
                .FromFileInput(sourcePath)
                .OutputToFile(outputPath, overwrite: false, options => options
                    .WithAudioCodec("libvorbis")
                    .WithCustomArgument("-qscale:a 5")
                    .WithCustomArgument("-vn"))
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
