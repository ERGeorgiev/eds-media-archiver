using EdsMediaArchiver.Models;
using EdsMediaArchiver.Services.Logging;

namespace EdsMediaArchiver.Services.Processors;

public interface IArchiveProcessor
{
    Task ProcessFileAsync(ArchiveRequest request);
}

/// <summary>
/// Orchestrates file processing by delegating to specialized processors
/// based on the user's preferences set on the request.
/// </summary>
public class ArchiveProcessor(
    ICompressProcessor compressProcessor,
    IConvertProcessor convertProcessor,
    IDateProcessor dateProcessor,
    IProcessLogger processLogger) : IArchiveProcessor
{
    public async Task ProcessFileAsync(ArchiveRequest request)
    {
        try
        {
            if (Constants.FileTypeToExtension.TryGetValue(request.ActualFileType, out var actualExtension))
            {
                if (Constants.SupportedExtensions.Contains(actualExtension) == false)
                {
                    processLogger.Log(IProcessLogger.Operation.Archive, IProcessLogger.Result.Skip, request.OriginalPath.Absolute, $"Unsupported FileType '{request.ActualFileType}'");
                    return;
                }
            }

            if (request.Compress)
            {
                var compressedFilePath = await compressProcessor.ProcessAsync(request.NewPath.Absolute, request.NewPath.Directory, request.ActualFileType);
                request.NewPath = new(request.NewPath.Root, compressedFilePath);
            }
            
            // ToDo: Instead of this, do a Compress/Standardize(if not compress)/SetDates
            // That way, no need to wonder what supports EXIF what doesn't, what is read by a gallery what isn't.
            if (request.ConvertIfUnreliableForDates && IDateProcessor.IsReliableFileTypeForDate(request.ActualFileType))
            {
                var convertedFilePath = await convertProcessor.ProcessAsync(request.NewPath.Absolute, request.NewPath.Directory, request.ActualFileType);
                request.NewPath = new(request.NewPath.Root, convertedFilePath);
            }

            if (request.SetDates)
            {
                var dateResult = await dateProcessor.ProcessAsync(request.FileInfo, request.NewPath.Directory, request.ActualFileType);
            }
        }
        catch (Exception ex)
        {
            processLogger.Log(IProcessLogger.Operation.Archive, IProcessLogger.Result.Error, request.OriginalPath.Absolute, ex.Message);
        }
    }
}
