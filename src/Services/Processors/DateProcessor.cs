using EdsMediaArchiver.Models;

namespace EdsMediaArchiver.Services.Processors;

public interface IDateProcessor
{
    /// <summary>
    /// Writes date metadata and sets filesystem Created/Modified dates.
    /// Skips if no valid date is available.
    /// </summary>
    Task<ProcessingResult> ProcessAsync(ArchiveRequest request, string actualType);
}

public class DateProcessor(IMetadataWriter metadataWriter) : IDateProcessor
{
    public async Task<ProcessingResult> ProcessAsync(ArchiveRequest request, string actualType)
    {
        if (!request.OriginDate.HasValue)
        {
            Console.WriteLine($"  [SKIP] {request.NewPath.Relative} - no valid dates found");
            return new ProcessingResult(request.NewPath.Relative, null, ProcessingStatus.Skipped, "No valid dates found");
        }

        var filePath = request.NewPath.Absolute;
        var date = request.OriginDate.Value;

        await WriteDateForTypeAsync(filePath, actualType, date);
        SetFilesystemDates(filePath, date);

        Console.WriteLine($"  [OK] {request.NewPath.Relative} -> {date:yyyy-MM-dd HH:mm:ss}");
        return new ProcessingResult(request.NewPath.Relative, date, ProcessingStatus.Fixed);
    }

    private async Task WriteDateForTypeAsync(string filePath, string actualType, DateTimeOffset date)
    {
        if (MediaType.ExifWritableTypes.Contains(actualType))
            await metadataWriter.WriteExifDatesAsync(filePath, date);
        else if (MediaType.VideoTypes.Contains(actualType))
            metadataWriter.WriteVideoDates(filePath, date);
        else if (actualType.Equals("PNG", StringComparison.OrdinalIgnoreCase))
            await metadataWriter.WritePngDatesAsync(filePath, date);
        else
            await metadataWriter.WriteXmpDatesAsync(filePath, date);
    }

    private static void SetFilesystemDates(string filePath, DateTimeOffset date)
    {
        File.SetCreationTime(filePath, date.LocalDateTime);
        File.SetLastWriteTime(filePath, date.LocalDateTime);
    }
}
