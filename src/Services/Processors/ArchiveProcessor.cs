using EdsMediaArchiver.Definitions;
using EdsMediaArchiver.Models;
using EdsMediaArchiver.Services.Compressors;
using EdsMediaArchiver.Services.Logging;
using EdsMediaArchiver.Services.Resolvers;

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
    IDateProcessor dateProcessor,
    IFileExtensionResolver extensionRestorer,
    IProcessLogger processLogger) : IArchiveProcessor
{
    public async Task ProcessFileAsync(ArchiveRequest request)
    {
        try
        {
            if (MediaType.SupportedTypes.Contains(request.ActualFileType) == false)
            {
                processLogger.Log(IProcessLogger.Operation.Archive, IProcessLogger.Result.Skip, request.OriginalPath.Absolute, $"Unsupported FileType '{request.ActualFileType}'");
                return;
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
