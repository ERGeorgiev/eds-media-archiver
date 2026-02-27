namespace PersonalMediaArchiver.Services;

/// <summary>
/// Determines the best date for a media file using the priority:
///   1. DateTimeOriginal (EXIF) — wins unconditionally
///   2. Date embedded in filename (YYYYMMDD patterns)
///   3. Oldest from other trusted EXIF/XMP tags + filesystem dates
/// </summary>
public class DateResolver
{
    private readonly ExifToolService _exifTool;

    public DateResolver(ExifToolService exifTool)
    {
        _exifTool = exifTool;
    }

    public async Task<DateTime?> ResolveBestDateAsync(string filePath)
    {
        // 1. DateTimeOriginal wins unconditionally
        var dto = await _exifTool.GetDateTimeOriginalAsync(filePath);
        if (dto.HasValue) return dto;

        // 2. Filename date is second priority
        var filenameDate = ExtractDateFromFilename(Path.GetFileName(filePath));
        if (filenameDate.HasValue) return filenameDate;

        // 3. Oldest from other trusted tags + filesystem
        return await GetOldestCandidateDateAsync(filePath);
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

    private async Task<DateTime?> GetOldestCandidateDateAsync(string filePath)
    {
        var candidates = new List<DateTime>();

        // Other trusted EXIF/XMP tags
        var exifDates = await _exifTool.GetOtherTrustedDatesAsync(filePath);
        candidates.AddRange(exifDates);

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
