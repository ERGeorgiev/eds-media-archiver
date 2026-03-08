namespace EdsMediaArchiver.Services.Writers;

public interface IFileDateWriter
{
    /// </summary>
    Task<DateTimeOffset?> WriteDateToFileAsync(string filePath, string fileType, DateTimeOffset date);
}

public class FileDateWriter(IExifToolService exif) : IFileDateWriter
{
    public async Task<DateTimeOffset?> WriteDateToFileAsync(string filePath, string fileType, DateTimeOffset date)
    {
        try
        {
            await exif.WriteDatesAsync(filePath, date);
        }
        catch
        {
            // ToDo: Skip exception, as some formats are not supported by the code, but not all have been tested yet,
            // and its not critical as system dates and ffmpeg can handle it instead.
        }

        File.SetCreationTime(filePath, date.LocalDateTime);
        File.SetLastWriteTime(filePath, date.LocalDateTime);

        return date;
    }
}
