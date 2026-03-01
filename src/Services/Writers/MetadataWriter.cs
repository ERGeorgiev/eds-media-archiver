using ImageMagick;

namespace EdsMediaArchiver.Services.Writers;

public interface IMetadataWriter
{
    Task WriteImageDatesAsync(string filePath, DateTimeOffset date);
    void WriteVideoDates(string filePath, DateTimeOffset date);
    void WriteAudioDates(string filePath, DateTimeOffset date);
}

public class MetadataWriter(IXmpWriter xmpWriter) : IMetadataWriter
{
    public async Task WriteImageDatesAsync(string filePath, DateTimeOffset date)
    {
        using var image = new MagickImage();
        await image.ReadAsync(filePath);

        var exifDate = date.ToString("yyyy:MM:dd HH:mm:ss");

        // Update EXIF
        var exif = image.GetExifProfile() ?? new ExifProfile();
        exif.Parts = ExifParts.All; // Preserve EXIF thumbnail for Windows Explorer
        exif.SetValue(ExifTag.DateTimeOriginal, exifDate);
        exif.SetValue(ExifTag.DateTimeDigitized, exifDate);
        exif.SetValue(ExifTag.DateTime, exifDate);
        image.SetProfile(exif);

        // Update XMP (Safe Merge)
        xmpWriter.UpdateImageXmpProfile(image, date);

        if (Path.GetExtension(filePath).Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            // PNG Specific Attribute
            // tIME chunk is traditionally UTC
            image.SetAttribute("png:tIME", date.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        }

        // Use baseline encoding (not progressive) for fast Windows thumbnail generation
        image.Settings.Interlace = Interlace.NoInterlace;

        await image.WriteAsync(filePath);
    }

    public void WriteVideoDates(string filePath, DateTimeOffset date)
    {
        using var file = TagLib.File.Create(filePath);
        file.Tag.DateTagged = date.LocalDateTime;
        file.Tag.Year = (uint)date.Year;
        
        if (file is TagLib.Ogg.File oggFile)
        {
            oggFile.Tag.Year = (uint)date.Year;
            oggFile.Tag.DateTagged = date.LocalDateTime;
        }

        file.Save();
    }

    public void WriteAudioDates(string filePath, DateTimeOffset date)
    {
        using var file = TagLib.File.Create(filePath);

        file.Tag.DateTagged = date.LocalDateTime;
        file.Tag.Year = (uint)date.Year;

        if (file is TagLib.Mpeg4.File mp4File)
        {
            mp4File.Tag.DateTagged = date.UtcDateTime;
        }
        else if (file is TagLib.Matroska.File mkvFile)
        {
            mkvFile.Tag.DateTagged = date.UtcDateTime;
        }

        file.Save();
    }
}
