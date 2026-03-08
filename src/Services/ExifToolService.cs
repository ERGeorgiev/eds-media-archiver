using System.Diagnostics;
using System.Text;

namespace EdsMediaArchiver.Services;

public interface IExifToolService
{
    Task CopyMetadata(string sourceFilePath, string destinationFilePath);
    Task<bool> IsAlreadyTagged(string filePath);
    Task WriteTagsAsync(string filePath, IReadOnlyList<string> tags);
    Task WriteDatesAsync(string filePath, DateTimeOffset date);
}

public class ExifToolService : IExifToolService
{
    public async Task CopyMetadata(string sourceFilePath, string destinationFilePath)
    {
        var args = new List<string>
        {
            "-f",
            "-m",
            "-overwrite_original",
            "-tagsFromFile",
            sourceFilePath,
            "-all:all<all:all",
            destinationFilePath
        };

        await ExecuteAsync(args);
    }

    public async Task WriteTagsAsync(string filePath, IReadOnlyList<string> tags)
    {
        if (tags.Count == 0)
            return;

        var clears = new[]
        {
            "-XPKeywords=",
            "-Keywords=",
            "-Subject=",
            "-QuickTime:Category=",
            "-XMP-dc:Subject=",
            "-Microsoft:Category=",
        };

        var args = new List<string>(clears);

        foreach (var tag in tags)
            args.Add($"-Keywords={tag}");

        foreach (var tag in tags)
            args.Add($"-Subject={tag}");

        args.Add($"-XPKeywords={string.Join(";", tags)}");

        // QuickTime:Category is what Windows Explorer reads as "Tags"
        args.Add($"-QuickTime:Category={string.Join(";", tags)}");
        // Also write XMP:Subject for non-Windows tools (Lightroom, digiKam, etc.)
        foreach (var tag in tags)
            args.Add($"-XMP-dc:Subject={tag}");

        // This is what Windows Explorer reads as "Tags"
        args.Add($"-Microsoft:Category={string.Join(";", tags)}");

        args.Add("-overwrite_original");
        args.Add(filePath);

        args.Insert(0, "-f");
        args.Insert(1, "-m");

        await ExecuteAsync(args);
    }

    public async Task<bool> IsAlreadyTagged(string filePath)
    {
        var args = new List<string>
        {
            "-XPKeywords",
            "-Keywords",
            "-Subject",
            "-QuickTime:Category",
            "-XMP-dc:Subject",
            "-Microsoft:Category",
            "-s",   // short tag names
            filePath
        };
        var output = await ExecuteAsync(args);
        return output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split(':', 2))
            .Where(parts => parts.Length == 2)
            .Select(parts => parts[1].Trim())
            .Where(part => string.IsNullOrEmpty(part) == false)
            .Distinct()
            .Any();
    }

    public async Task WriteDatesAsync(string filePath, DateTimeOffset date)
    {
        var exifDate = date.ToString("yyyy:MM:dd HH:mm:ss");
        var xmpDate = date.ToString("yyyy-MM-ddTHH:mm:ssK");
        var utcDate = date.UtcDateTime.ToString("yyyy:MM:dd HH:mm:ss");
        var pngDate = date.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");

        var args = new List<string>
        {
            "-f",
            "-m",
            "-overwrite_original",

            // EXIF dates
            $"-EXIF:DateTimeOriginal={exifDate}",
            $"-EXIF:CreateDate={exifDate}",
            $"-EXIF:ModifyDate={exifDate}",

            // XMP dates
            $"-XMP-xmp:CreateDate={xmpDate}",
            $"-XMP-xmp:ModifyDate={xmpDate}",
            $"-XMP-xmp:MetadataDate={xmpDate}",
            $"-XMP-exif:DateTimeOriginal={xmpDate}",
            $"-XMP-exif:DateTimeDigitized={xmpDate}",
            $"-XMP-tiff:DateTime={exifDate}",

            // QuickTime dates (for MP4/MOV)
            $"-QuickTime:CreateDate={utcDate}",
            $"-QuickTime:ModifyDate={utcDate}",
            $"-QuickTime:TrackCreateDate={utcDate}",
            $"-QuickTime:TrackModifyDate={utcDate}",
            $"-QuickTime:MediaCreateDate={utcDate}",
            $"-QuickTime:MediaModifyDate={utcDate}",
            
            // PNG tIME chunk (traditionally UTC)
            $"-PNG:ModifyDate={pngDate}",

            filePath
        };

        await ExecuteAsync(args);
    }

    private static async Task<string> ExecuteAsync(IReadOnlyList<string> args)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "exiftool",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            }
        };

        // Use -@ - to read args from stdin, with UTF-8 filename charset
        process.StartInfo.ArgumentList.Add("-charset");
        process.StartInfo.ArgumentList.Add("filename=utf8");
        process.StartInfo.ArgumentList.Add("-@");
        process.StartInfo.ArgumentList.Add("-");

        process.Start();

        // Write args to stdin as UTF-8, one per line
        await using (var writer = new StreamWriter(
            process.StandardInput.BaseStream, new UTF8Encoding(false)))
            foreach (var arg in args)
                await writer.WriteLineAsync(arg);

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"ExifTool failed (exit code {process.ExitCode}): {error.Trim()}");

        return output;
    }
}
