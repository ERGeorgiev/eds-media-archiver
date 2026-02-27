using System.Text.RegularExpressions;

namespace PersonalMediaArchiver.Services;

/// <summary>
/// Determines the best date for a media file using the priority:
///   1. DateTimeOriginal (EXIF) — wins unconditionally
///   2. Date embedded in filename (YYYYMMDD patterns)
///   3. Unix timestamp embedded in filename
///   4. Oldest from other trusted EXIF/XMP tags + filesystem dates
/// </summary>
public class DateResolver
{
    private readonly MetadataService _metadata;

    private static readonly Regex TimestampMillisPattern =
        new(@"(?<!\d)(\d{13})(?!\d)", RegexOptions.Compiled);

    private static readonly Regex TimestampSecondsPattern =
        new(@"(?<!\d)(\d{10})(?!\d)", RegexOptions.Compiled);

    public DateResolver(MetadataService metadata)
    {
        _metadata = metadata;
    }

    public DateTime? ResolveBestDate(string filePath)
    {
        // 1. DateTimeOriginal wins unconditionally
        var dto = _metadata.GetDateTimeOriginal(filePath);
        if (dto.HasValue) return dto;

        // 2. Filename date is second priority
        var filenameDate = ExtractDateFromFilename(Path.GetFileName(filePath));
        if (filenameDate.HasValue) return filenameDate;

        // 3. Unix timestamp in filename
        var timestampDate = ExtractTimestampFromFilename(Path.GetFileName(filePath));
        if (timestampDate.HasValue) return timestampDate;

        // 4. Oldest from other trusted tags + filesystem
        return GetOldestCandidateDate(filePath);
    }

    public static DateTime? ExtractDateFromFilename(string fileName)
    {
        foreach (var pattern in Constants.FilenamePatterns)
        {
            var match = pattern.Match(fileName);
            if (!match.Success) continue;

            try
            {
                int year  = int.Parse(match.Groups["y"].Value);
                int month = int.Parse(match.Groups["m"].Value);
                int day   = int.Parse(match.Groups["d"].Value);
                int hour  = match.Groups["H"].Success   ? int.Parse(match.Groups["H"].Value)   : 12;
                int min   = match.Groups["Min"].Success ? int.Parse(match.Groups["Min"].Value) : 0;
                int sec   = match.Groups["Sec"].Success ? int.Parse(match.Groups["Sec"].Value) : 0;

                var dt = new DateTime(year, month, day, hour, min, sec);
                if (dt <= DateTime.Now.AddDays(1))
                    return dt;
            }
            catch
            {
                // Invalid date components — skip
            }

            break; // Only try the first matching pattern
        }

        return null;
    }

    /// <summary>
    /// Extracts a date from a Unix epoch timestamp embedded in the filename.
    /// Supports 13-digit millisecond timestamps and 10-digit second timestamps.
    /// Only considers timestamps that resolve to dates from year 2000 onward.
    /// </summary>
    public static DateTime? ExtractTimestampFromFilename(string fileName)
    {
        // Try 13-digit millisecond timestamp first (more specific)
        var match = TimestampMillisPattern.Match(fileName);
        if (match.Success && long.TryParse(match.Groups[1].Value, out var ms))
        {
            var dt = DateTimeOffset.FromUnixTimeMilliseconds(ms).LocalDateTime;
            if (dt.Year >= 2000 && dt <= DateTime.Now.AddDays(1))
                return dt;
        }

        // Try 10-digit second timestamp
        match = TimestampSecondsPattern.Match(fileName);
        if (match.Success && long.TryParse(match.Groups[1].Value, out var sec))
        {
            var dt = DateTimeOffset.FromUnixTimeSeconds(sec).LocalDateTime;
            if (dt.Year >= 2000 && dt <= DateTime.Now.AddDays(1))
                return dt;
        }

        return null;
    }

    private DateTime? GetOldestCandidateDate(string filePath)
    {
        var candidates = new List<DateTime>();

        // Other trusted EXIF/XMP tags
        var metadataDates = _metadata.GetOtherTrustedDates(filePath);
        candidates.AddRange(metadataDates);

        // Filesystem dates
        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Exists)
        {
            AddIfValid(candidates, fileInfo.CreationTime);
            AddIfValid(candidates, fileInfo.LastWriteTime);
        }

        return candidates.Count > 0 ? candidates.Min() : null;
    }

    private static void AddIfValid(List<DateTime> candidates, DateTime dt)
    {
        if (dt <= DateTime.Now.AddDays(1))
            candidates.Add(dt);
    }
}
