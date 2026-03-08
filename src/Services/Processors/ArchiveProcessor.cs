using EdsMediaArchiver.Definitions;
using EdsMediaArchiver.Models;
using EdsMediaArchiver.Services.Compressors;
using EdsMediaArchiver.Services.Logging;
using EdsMediaArchiver.Services.Resolvers;
using EdsMediaArchiver.Services.Writers;
using MetadataExtractor;

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
    IFileDateWriter dateProcessor,
    IFileExtensionResolver extensionRestorer,
    IFileDateResolver fileDateResolver,
    IProcessLogger processLogger) : IArchiveProcessor
{
    public async Task ProcessFileAsync(ArchiveRequest request)
    {
        try
        {
            if (MediaType.SupportedTypes.Contains(request.ActualFileType) == false)
            {
                processLogger.Log(IProcessLogger.Operation.Archive, IProcessLogger.Result.SKIPPED, request.OriginalPath.Absolute, $"Unsupported FileType '{request.ActualFileType}'");
                return;
            }

            DateTimeOffset? setDate = null;
            if (request.SetDates)
            {
                setDate = fileDateResolver.ResolveBestDate(request.ActualFileType, request.OriginalPath.Absolute);
            }

            if (request.Compress)
            {
                var mode = request.ReizeOnCompress ? CompressorMode.CompressAndResize : CompressorMode.Compress;
                var output = await compressProcessor.ProcessAsync(request.NewPath.Absolute, request.NewPath.Directory, request.ActualFileType, mode);
                request.NewPath = new(request.NewPath.Root, output);
            }
            else if (request.Standardize)
            {
                var output = await extensionRestorer.RestoreExtension(request.NewPath.Absolute, request.NewPath.Directory, request.ActualFileType);
                request.NewPath = new(request.NewPath.Root, output);
                output = await compressProcessor.ProcessAsync(request.NewPath.Absolute, request.NewPath.Directory, request.ActualFileType, CompressorMode.Convert);
                request.NewPath = new(request.NewPath.Root, output);
                // ToDo: Update actual type? SetDates later relies on it!
            }

            if (request.SetDates)
            {
                if (setDate == null)
                {
                    processLogger.Log(IProcessLogger.Operation.SetDate, IProcessLogger.Result.SKIPPED, request.OriginalPath.Absolute, "No valid dates found.");
                }
                else
                {
                    var dateResult = await dateProcessor.WriteDateToFileAsync(request.NewPath.Absolute, request.ActualFileType, setDate.Value);
                    processLogger.Log(IProcessLogger.Operation.SetDate, IProcessLogger.Result.SUCCESS, request.OriginalPath.Absolute, $"{setDate:yyyy-MM-dd HH:mm:ss}");
                }
            }
        }
        catch (Exception ex)
        {
            processLogger.Log(IProcessLogger.Operation.Archive, IProcessLogger.Result.ERROR, request.OriginalPath.Absolute, ex.Message);
        }
    }
}
