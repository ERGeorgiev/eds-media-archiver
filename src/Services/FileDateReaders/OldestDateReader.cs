using EdsMediaArchiver.Services.Validators;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;
using MetadataExtractor.Formats.Xmp;
using System.Globalization;

namespace EdsMediaArchiver.Services.FileDateReaders;

public interface IOldestDateReader : IFileDateReader { }

public partial class OldestDateReader(IDateValidator dateValidator) : IOldestDateReader
{
    /// <summary>
    /// Reads other trusted date tags: EXIF CreateDate/ModifyDate,
    /// QuickTime movie/track dates, and XMP dates.
    /// </summary>
    public DateTimeOffset? Read(string filePath, IEnumerable<MetadataExtractor.Directory> fileDirectories)
    {
        var dates = new List<DateTimeOffset>();
        // The QuickTime "Zero" date: Jan 1, 1904
        var quickTimeEpoch = new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        foreach (var dir in fileDirectories)
        {
            try
            {
                switch (dir)
                {
                    case ExifSubIfdDirectory subIfd:
                        TryAdd(TryGetDate(subIfd, ExifDirectoryBase.TagDateTimeDigitized));
                        break;

                    case ExifIfd0Directory ifd0:
                        TryAdd(TryGetDate(ifd0, ExifIfd0Directory.TagDateTime));
                        break;

                    case QuickTimeMovieHeaderDirectory movHeader:
                        var movCreate = TryGetDate(movHeader, QuickTimeMovieHeaderDirectory.TagCreated);
                        if (movCreate.HasValue && movCreate.Value.UtcDateTime > quickTimeEpoch)
                            TryAdd(movCreate);

                        var movMod = TryGetDate(movHeader, QuickTimeMovieHeaderDirectory.TagModified);
                        if (movMod.HasValue && movMod.Value.UtcDateTime > quickTimeEpoch)
                            TryAdd(movMod);
                        break;

                    case QuickTimeTrackHeaderDirectory trackHeader:
                        var trackCreate = TryGetDate(trackHeader, QuickTimeTrackHeaderDirectory.TagCreated);
                        if (trackCreate.HasValue && trackCreate.Value.UtcDateTime > quickTimeEpoch)
                            TryAdd(trackCreate);

                        var trackMod = TryGetDate(trackHeader, QuickTimeTrackHeaderDirectory.TagModified);
                        if (trackMod.HasValue && trackMod.Value.UtcDateTime > quickTimeEpoch)
                            TryAdd(trackMod);
                        break;

                    case XmpDirectory xmp:
                        TryAdd(GetXmpDate(xmp, "http://ns.adobe.com/xap/1.0/", "CreateDate"));
                        TryAdd(GetXmpDate(xmp, "http://ns.adobe.com/xap/1.0/", "ModifyDate"));
                        TryAdd(GetXmpDate(xmp, "http://ns.adobe.com/exif/1.0/", "DateTimeDigitized"));
                        break;
                }
            }
            catch { /* Skip corrupted directory */ }
        }

        if (dates.Count == 0)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                TryAdd(fileInfo.CreationTime);
                TryAdd(fileInfo.LastWriteTime);
            }
            catch { }
        }

        return dates.Count > 0 ? dates.Min() : null;

        void TryAdd(DateTimeOffset? date)
        {
            if (date != null && dateValidator.IsValid(date))
                dates.Add(date.Value);
        }
    }

    private DateTimeOffset? TryGetDate(MetadataExtractor.Directory dir, int tag)
    {
        if (dir.TryGetDateTime(tag, out var dt) && dateValidator.IsValid(dt))
        {
            return dt;
        }
        return null;
    }

    private static DateTimeOffset? GetXmpDate(XmpDirectory xmpDir, string ns, string property)
    {
        try
        {
            var value = xmpDir.XmpMeta?.GetPropertyString(ns, property);
            if (string.IsNullOrWhiteSpace(value)) return null;

            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal, out var parsed))
                return parsed;
        }
        catch { }
        return null;
    }
}
