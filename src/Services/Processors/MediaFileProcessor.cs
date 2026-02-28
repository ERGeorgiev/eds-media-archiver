using EdsMediaArchiver.Models;
using EdsMediaArchiver.Services.Resolvers;

namespace EdsMediaArchiver.Services.Processors;

public interface IMediaFileProcessor
{
    Task<ProcessingResult> ProcessFileAsync(ArchiveRequest request);
}

/// <summary>
/// Orchestrates file processing by delegating to specialized processors
/// based on the user's preferences set on the request.
/// </summary>
public class MediaFileProcessor(
    IFileTypeResolver fileTypeService,
    IExtensionFixProcessor extensionFixProcessor,
    ICompressProcessor compressProcessor,
    IDateProcessor dateProcessor) : IMediaFileProcessor
{
    public async Task<ProcessingResult> ProcessFileAsync(ArchiveRequest request)
    {
        try
        {
            var actualType = fileTypeService.GetFileType(request.NewPath.Absolute);

            if (request.FixExtension)
                extensionFixProcessor.Process(request, actualType);

            if (request.Compress)
            {
                var result = await compressProcessor.ProcessAsync(request, actualType);
                if (result != null) return result;
            }

            if (request.SetDates)
                return await dateProcessor.ProcessAsync(request, actualType);

            // Only extension was fixed — detect via path change
            if (request.NewPath.Absolute != request.OriginalPath.Absolute)
                return new ProcessingResult(request.NewPath.Relative, null, ProcessingStatus.Renamed);

            Console.WriteLine($"  [SKIP] {request.NewPath.Relative} - no applicable processing");
            return new ProcessingResult(request.NewPath.Relative, null, ProcessingStatus.Skipped, "No applicable processing");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [ERR] {request.NewPath.Relative} - {ex.Message}");
            return new ProcessingResult(request.NewPath.Relative, null, ProcessingStatus.Error, ex.Message);
        }
    }
}
