using System.Globalization;

namespace PersonalMediaArchiver.Services;

/// <summary>
/// Wrapper around the exiftool CLI for reading and writing media metadata.
/// </summary>
public class ExifToolService
{
    public static bool IsAvailable() => ProcessRunner.IsAvailable("exiftool", "-ver");

    public async Task<string> GetFileTypeAsync(string filePath)
    {
        var (output, _) = await ProcessRunner.RunAsync(
            "exiftool", "-s", "-s", "-s", "-FileType", filePath);
        var type = output.Trim();
        return string.IsNullOrWhiteSpace(type) ? "UNKNOWN" : type;
    }

    public async Task<DateTime?> GetDateTimeOriginalAsync(string filePath)
    {
        var (output, _) = await ProcessRunner.RunAsync(
            "exiftool", "-s", "-s", "-s", "-DateTimeOriginal",
            "-d", "%Y-%m-%dT%H:%M:%S", filePath);
        return ParseExifDate(output);
    }

    public async Task<List<DateTime>> GetOtherTrustedDatesAsync(string filePath)
    {
        var args = new List<string> { "-s", "-s", "-s" };
        args.AddRange(new[]
        {
            "-CreateDate", "-MediaCreateDate", "-TrackCreateDate",
            "-XMP:DateTimeOriginal", "-XMP:CreateDate", "-XMP:ModifyDate"
        });
        args.AddRange(new[] { "-d", "%Y-%m-%dT%H:%M:%S", filePath });

        var (output, _) = await ProcessRunner.RunAsync("exiftool", args.ToArray());

        var dates = new List<DateTime>();
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parsed = ParseExifDate(line);
            if (parsed.HasValue)
                dates.Add(parsed.Value);
        }
        return dates;
    }

    public async Task WriteExifDatesAsync(string filePath, DateTime date)
    {
        var dateStr = date.ToString("yyyy:MM:dd HH:mm:ss");
        await ProcessRunner.RunAsync("exiftool", "-overwrite_original", "-q",
            $"-DateTimeOriginal={dateStr}", $"-CreateDate={dateStr}", $"-ModifyDate={dateStr}",
            filePath);
    }

    public async Task WriteVideoDatesAsync(string filePath, DateTime date)
    {
        var dateStr = date.ToString("yyyy:MM:dd HH:mm:ss");
        await ProcessRunner.RunAsync("exiftool", "-overwrite_original", "-q",
            $"-CreateDate={dateStr}", $"-ModifyDate={dateStr}",
            $"-MediaCreateDate={dateStr}", $"-MediaModifyDate={dateStr}",
            $"-TrackCreateDate={dateStr}", $"-TrackModifyDate={dateStr}",
            filePath);
    }

    public async Task WritePngDatesAsync(string filePath, DateTime date)
    {
        var dateStr = date.ToString("yyyy:MM:dd HH:mm:ss");
        await ProcessRunner.RunAsync("exiftool", "-overwrite_original", "-q",
            $"-DateTimeOriginal={dateStr}", $"-CreateDate={dateStr}", $"-png:tIME={dateStr}",
            filePath);
    }

    public async Task WriteXmpDatesAsync(string filePath, DateTime date)
    {
        var dateStr = date.ToString("yyyy:MM:dd HH:mm:ss");
        await ProcessRunner.RunAsync("exiftool", "-overwrite_original", "-q",
            $"-XMP:CreateDate={dateStr}", $"-XMP:ModifyDate={dateStr}",
            $"-XMP:DateTimeOriginal={dateStr}",
            filePath);
    }

    private static DateTime? ParseExifDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        if (DateTime.TryParseExact(trimmed, "yyyy-MM-ddTHH:mm:ss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            && parsed <= DateTime.Now.AddDays(1))
        {
            return parsed;
        }
        return null;
    }
}
