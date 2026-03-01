using EdsMediaArchiver.Services.Validators;
using System.Text.RegularExpressions;

namespace EdsMediaArchiver.Services.FileDateReaders;

public interface IFilenameDateReader : IFileDateReader { }

public partial class FilenameDateReader(IDateValidator dateValidator) : IFilenameDateReader
{
    public DateTimeOffset? Read(string filePath, IEnumerable<MetadataExtractor.Directory> fileDirectories)
    {
        var filename = Path.GetFileNameWithoutExtension(filePath);
        Match? match;
        foreach (var pattern in FilenameDateTimePatterns)
        {
            match = pattern.Match(filename);
            if (!match.Success) continue;

            try
            {
                int year = int.Parse(match.Groups["y"].Value);
                int month = int.Parse(match.Groups["m"].Value);
                int day = int.Parse(match.Groups["d"].Value);
                int hour = match.Groups["H"].Success ? int.Parse(match.Groups["H"].Value) : 12;
                int min = match.Groups["Min"].Success ? int.Parse(match.Groups["Min"].Value) : 0;
                int sec = match.Groups["Sec"].Success ? int.Parse(match.Groups["Sec"].Value) : 0;

                var dt = new DateTime(year, month, day, hour, min, sec);
                if (dateValidator.IsValid(dt))
                    return dt;
            }
            catch { }
        }

        // Try 10-digit second timestamp
        match = TimestampSecondsPattern().Match(filename);
        if (match.Success && long.TryParse(match.Groups[1].Value, out var unixSec))
        {
            var dt = DateTimeOffset.FromUnixTimeSeconds(unixSec).LocalDateTime;
            if (dateValidator.IsValid(dt))
                return dt;
        }

        // Try 13-digit millisecond timestamp first (more specific)
        match = TimestampMillisPattern().Match(filename);
        if (match.Success && long.TryParse(match.Groups[1].Value, out var unixMs))
        {
            var dt = DateTimeOffset.FromUnixTimeMilliseconds(unixMs).LocalDateTime;
            if (dateValidator.IsValid(dt))
                return dt;
        }

        return null;
    }

    /// <summary>
    /// Regex patterns for dates embedded in filenames.
    /// Matches YYYYMMDD with optional separators, optionally followed by HHmmss.
    /// Covers: 20231225, 2023-12-25, IMG_20231225_143022, etc.
    /// </summary>
    private static readonly Regex[] FilenameDateTimePatterns =
    {
        // YYYY[sep]MM[sep]DD[sep]HH[sep]mm[sep]ss (with time)
        DateTimePattern(),
        // YYYY[sep]MM[sep]DD (date only)
        DateOnlyPattern()
    };

    [GeneratedRegex(@"(?:^|[\s_\-\.\(~])(?<y>20\d{2})[_\-\.]?(?<m>[01]\d)[_\-\.]?(?<d>[0-3]\d)[_\-\.]?(?<H>[0-2]\d)[_\-\.]?(?<Min>[0-5]\d)[_\-\.]?(?<Sec>[0-5]\d)(?:$|[\s_\-\.\(\)~])", RegexOptions.Compiled)]
    private static partial Regex DateTimePattern();

    [GeneratedRegex(@"(?:^|[\s_\-\.\(~])(?<y>20\d{2})[_\-\.]?(?<m>[01]\d)[_\-\.]?(?<d>[0-3]\d)(?:$|[\s_\-\.\(\)~])", RegexOptions.Compiled)]
    private static partial Regex DateOnlyPattern();

    [GeneratedRegex(@"(?<!\d)(\d{10})(?!\d)", RegexOptions.Compiled)]
    private static partial Regex TimestampSecondsPattern();

    [GeneratedRegex(@"(?<!\d)(\d{13})(?!\d)", RegexOptions.Compiled)]
    private static partial Regex TimestampMillisPattern();
}
