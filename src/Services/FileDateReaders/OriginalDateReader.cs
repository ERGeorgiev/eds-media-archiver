using EdsMediaArchiver.Services.Validators;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Xmp;
using System.Globalization;

namespace EdsMediaArchiver.Services.FileDateReaders;

public interface IOriginalDateReader : IFileDateReader { }

public class OriginalDateReader(IDateValidator dateValidator) : IOriginalDateReader
{
    public DateTimeOffset? Read(string filePath, IEnumerable<MetadataExtractor.Directory> fileDirectories)
    {
        try
        {
            var exifSubIfd = fileDirectories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            if (exifSubIfd != null
                && exifSubIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var dto)
                && dateValidator.IsValid(dto))
            {
                return dto;
            }

            // XMP DateTimeOriginal fallback
            foreach (var xmpDir in fileDirectories.OfType<XmpDirectory>())
            {
                try
                {
                    var value = xmpDir.XmpMeta?.GetPropertyString("http://ns.adobe.com/exif/1.0/", "DateTimeOriginal");
                    if (string.IsNullOrWhiteSpace(value)) return null;

                    if (DateTime.TryParse(value, CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeLocal, out var parsed)
                        && dateValidator.IsValid(parsed))
                        return parsed;
                }
                catch { }
            }
        }
        catch { }
        return null;
    }
}
