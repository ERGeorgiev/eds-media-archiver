using ImageMagick;

namespace EdsMediaArchiver.Services;

public interface IMetadataWriter
{
    void WriteAudioDates(string filePath, DateTimeOffset date);
    Task WriteExifDatesAsync(string filePath, DateTimeOffset date);
    Task WritePngDatesAsync(string filePath, DateTimeOffset date);
    void WriteVideoDates(string filePath, DateTimeOffset date);
    Task WriteXmpDatesAsync(string filePath, DateTimeOffset date);
}

public class MetadataWriter : IMetadataWriter
{
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
        image.SetAttribute("png:tIME", date.ToString("yyyy-MM-ddTHH:mm:ssZ"));

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

    public void WriteVideoDates(string filePath, DateTimeOffset date)
    {
        using var file = TagLib.File.Create(filePath);
        file.Tag.DateTagged = date.LocalDateTime;
        file.Tag.Year = (uint)date.LocalDateTime.Year;
        file.Save();
    }

    public void WriteAudioDates(string filePath, DateTimeOffset date)
    {
        using var file = TagLib.File.Create(filePath);
        file.Tag.DateTagged = date.LocalDateTime;
        file.Tag.Year = (uint)date.LocalDateTime.Year;
        file.Save();
    }
}
