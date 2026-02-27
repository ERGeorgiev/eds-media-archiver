using System.Globalization;
using ImageMagick;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;
using MetadataExtractor.Formats.Xmp;
using MetadataExtractor.Util;

namespace EdsMediaArchiver.Services;

/// <summary>
/// Reads and writes media metadata using MetadataExtractor (reading),
/// Magick.NET (image writing), and TagLibSharp (video writing).
/// Replaces the exiftool CLI dependency.
/// </summary>
public class MetadataService
{
    /// <summary>
    /// Detects the actual file type using magic bytes via MetadataExtractor,
    /// returning an uppercase type string matching the convention used by Constants.
    /// Falls back to extension-based inference for types MetadataExtractor cannot detect.
    /// </summary>
    public string DetectFileType(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            var fileType = FileTypeDetector.DetectFileType(stream);

            // MetadataExtractor reports all HEIF-family files as Heif;
            // preserve HEIC vs HEIF distinction using the file extension.
            if (fileType == FileType.Heif)
            {
                var ext = Path.GetExtension(filePath);
                return ext.Equals(".heic", StringComparison.OrdinalIgnoreCase) ? "HEIC" : "HEIF";
            }

            if (fileType != FileType.Unknown)
                return NormalizeFileType(fileType);
        }
        catch { }

        return InferTypeFromExtension(filePath);
    }

    /// <summary>Reads the EXIF DateTimeOriginal tag (highest priority date source).</summary>
    public DateTimeOffset? GetDateTimeOriginal(string filePath)
    {
        try
        {
            var directories = ImageMetadataReader.ReadMetadata(filePath);

            var exifSubIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            if (exifSubIfd != null &&
                exifSubIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var dto))
            {
                return ValidateDate(dto);
            }

            // XMP DateTimeOriginal fallback
            foreach (var xmpDir in directories.OfType<XmpDirectory>())
            {
                var date = GetXmpDate(xmpDir, "http://ns.adobe.com/exif/1.0/", "DateTimeOriginal");
                if (date.HasValue) return date;
            }
        }
        catch { }
        return null;
    }

    /// <summary>
    /// Reads other trusted date tags: EXIF CreateDate/ModifyDate,
    /// QuickTime movie/track dates, and XMP dates.
    /// </summary>
    public List<DateTimeOffset> GetOtherTrustedDates(string filePath)
    {
        var dates = new List<DateTimeOffset>();
        try
        {
            var directories = ImageMetadataReader.ReadMetadata(filePath);

            // EXIF dates
            foreach (var subIfd in directories.OfType<ExifSubIfdDirectory>())
                TryAddDate(dates, subIfd, ExifDirectoryBase.TagDateTimeDigitized);

            foreach (var ifd0 in directories.OfType<ExifIfd0Directory>())
                TryAddDate(dates, ifd0, ExifDirectoryBase.TagDateTime);

            // QuickTime movie header
            foreach (var qtDir in directories.OfType<QuickTimeMovieHeaderDirectory>())
            {
                TryAddDate(dates, qtDir, QuickTimeMovieHeaderDirectory.TagCreated);
                TryAddDate(dates, qtDir, QuickTimeMovieHeaderDirectory.TagModified);
            }

            // QuickTime track header
            foreach (var qtDir in directories.OfType<QuickTimeTrackHeaderDirectory>())
            {
                TryAddDate(dates, qtDir, QuickTimeTrackHeaderDirectory.TagCreated);
                TryAddDate(dates, qtDir, QuickTimeTrackHeaderDirectory.TagModified);
            }

            // XMP dates
            foreach (var xmpDir in directories.OfType<XmpDirectory>())
            {
                AddXmpDate(dates, xmpDir, "http://ns.adobe.com/xap/1.0/", "CreateDate");
                AddXmpDate(dates, xmpDir, "http://ns.adobe.com/xap/1.0/", "ModifyDate");
                AddXmpDate(dates, xmpDir, "http://ns.adobe.com/exif/1.0/", "DateTimeDigitized");
            }
        }
        catch { }
        return dates;
    }

    // ── Writing (images via Magick.NET) ──────────────────────────────────

    public async Task WriteExifDatesAsync(string filePath, DateTimeOffset date)
    {
        using var image = new MagickImage();
        await image.ReadAsync(filePath);

        var profile = image.GetExifProfile() ?? new ExifProfile();
        var dateStr = date.ToString("yyyy:MM:dd HH:mm:ss");

        profile.SetValue(ExifTag.DateTimeOriginal, dateStr);
        profile.SetValue(ExifTag.DateTimeDigitized, dateStr);
        profile.SetValue(ExifTag.DateTime, dateStr);

        image.SetProfile(profile);
        await image.WriteAsync(filePath);
    }

    public async Task WritePngDatesAsync(string filePath, DateTimeOffset date)
    {
        using var image = new MagickImage();
        await image.ReadAsync(filePath);

        var profile = image.GetExifProfile() ?? new ExifProfile();
        var dateStr = date.ToString("yyyy:MM:dd HH:mm:ss");

        profile.SetValue(ExifTag.DateTimeOriginal, dateStr);
        profile.SetValue(ExifTag.DateTimeDigitized, dateStr);

        image.SetProfile(profile);
        await image.WriteAsync(filePath);
    }

    public async Task WriteXmpDatesAsync(string filePath, DateTimeOffset date)
    {
        using var image = new MagickImage();
        await image.ReadAsync(filePath);

        var isoStr = date.ToString("yyyy-MM-ddTHH:mm:ss");
        var xmpXml =
            "<?xpacket begin='' id='W5M0MpCehiHzreSzNTczkc9d'?>" +
            "<x:xmpmeta xmlns:x='adobe:ns:meta/'>" +
            "<rdf:RDF xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#'>" +
            "<rdf:Description rdf:about='' " +
            "xmlns:xmp='http://ns.adobe.com/xap/1.0/' " +
            "xmlns:exif='http://ns.adobe.com/exif/1.0/' " +
            $"xmp:CreateDate='{isoStr}' " +
            $"xmp:ModifyDate='{isoStr}' " +
            $"exif:DateTimeOriginal='{isoStr}'/>" +
            "</rdf:RDF>" +
            "</x:xmpmeta>" +
            "<?xpacket end='w'?>";

        image.SetProfile(new XmpProfile(System.Text.Encoding.UTF8.GetBytes(xmpXml)));
        await image.WriteAsync(filePath);
    }

    // ── Writing (video via TagLibSharp) ──────────────────────────────────

    public void WriteVideoDates(string filePath, DateTimeOffset date)
    {
        using var file = TagLib.File.Create(filePath);
        file.Tag.DateTagged = date.LocalDateTime;
        file.Tag.Year = (uint)date.LocalDateTime.Year;
        file.Save();
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static void TryAddDate(List<DateTimeOffset> dates, MetadataExtractor.Directory dir, int tag)
    {
        if (dir.TryGetDateTime(tag, out var dt))
        {
            var validated = ValidateDate(dt);
            if (validated.HasValue)
                dates.Add(validated.Value);
        }
    }

    private static DateTimeOffset? GetXmpDate(XmpDirectory xmpDir, string ns, string property)
    {
        try
        {
            var value = xmpDir.XmpMeta?.GetPropertyString(ns, property);
            if (string.IsNullOrWhiteSpace(value)) return null;

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal, out var parsed))
                return ValidateDate(parsed);
        }
        catch { }
        return null;
    }

    private static void AddXmpDate(List<DateTimeOffset> dates, XmpDirectory xmpDir, string ns, string property)
    {
        var date = GetXmpDate(xmpDir, ns, property);
        if (date.HasValue) dates.Add(date.Value);
    }

    private static DateTime? ValidateDate(DateTime dt)
    {
        return dt <= DateTime.Now.AddDays(1) ? dt : null;
    }

    private static string NormalizeFileType(FileType fileType)
    {
        return fileType switch
        {
            FileType.Jpeg => MediaType.Jpeg,
            FileType.Png => MediaType.Png,
            FileType.Gif => MediaType.Gif,
            FileType.Bmp => MediaType.Bmp,
            FileType.Tiff => MediaType.Tiff,
            FileType.WebP => MediaType.Webp,
            FileType.Heif => MediaType.Heif,
            FileType.QuickTime => MediaType.QuickTime,
            FileType.Mp4 => MediaType.Mp4,
            FileType.Avi => MediaType.Avi,
            _ => fileType.ToString().ToUpperInvariant()
        };
    }

    private static string InferTypeFromExtension(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return Constants.ExtensionToFileType.TryGetValue(ext, out var fileType) ? fileType : MediaType.Unknown;
    }
}
