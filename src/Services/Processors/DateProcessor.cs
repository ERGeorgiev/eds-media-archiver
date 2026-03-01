using EdsMediaArchiver.Definitions;
using EdsMediaArchiver.Services.Logging;
using EdsMediaArchiver.Services.Resolvers;
using EdsMediaArchiver.Services.Writers;
using MetadataExtractor;

namespace EdsMediaArchiver.Services.Processors;

public interface IDateProcessor
{
    /// <summary>
    /// Writes date metadata and sets filesystem Created/Modified dates.
    /// Determines the current file type from the file path (handles post-compression type changes).
    /// Skips if no valid date is available.
    /// </summary>
    Task<DateTimeOffset?> ProcessAsync(string filePath, string outputDirectory, string actualType);
}

public class DateProcessor(
    IMetadataWriter metadataWriter, 
    IFileTypeResolver fileTypeResolver, 
    IFileDateResolver fileDateResolver,
    IProcessLogger processLogger) : IDateProcessor
{
    public async Task<DateTimeOffset?> ProcessAsync(string filePath, string outputDirectory, string actualType)
    {
        var metadataDirectories = ImageMetadataReader.ReadMetadata(filePath);
        var originDate = fileDateResolver.ResolveBestDate(filePath, metadataDirectories);
        if (originDate.HasValue == false)
        {
            processLogger.Log(IProcessLogger.Operation.SetDate, IProcessLogger.Result.SKIPPED, filePath, "No valid dates found.");
            return null;
        }

        var currentType = fileTypeResolver.GetActualFileType(filePath);

        await WriteDateForTypeAsync(filePath, currentType, originDate.Value);
        SetFilesystemDates(filePath, originDate.Value);

        processLogger.Log(IProcessLogger.Operation.SetDate, IProcessLogger.Result.SUCCESS, filePath, $"{originDate:yyyy-MM-dd HH:mm:ss}");
        return originDate;
    }

    private async Task WriteDateForTypeAsync(string filePath, string actualType, DateTimeOffset date)
    {
        if (MediaType.SupportedImageTypes.Contains(actualType))
            await metadataWriter.WriteImageDatesAsync(filePath, date);
        else if (MediaType.SupportedVideoTypes.Contains(actualType))
            metadataWriter.WriteVideoDates(filePath, date);
        else if (MediaType.SupportedAudioTypes.Contains(actualType))
            metadataWriter.WriteAudioDates(filePath, date);
    }

    private static void SetFilesystemDates(string filePath, DateTimeOffset date)
    {
        File.SetCreationTime(filePath, date.LocalDateTime);
        File.SetLastWriteTime(filePath, date.LocalDateTime);
    }
}
