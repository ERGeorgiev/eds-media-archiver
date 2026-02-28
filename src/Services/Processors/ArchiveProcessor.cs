using EdsMediaArchiver.Models;

namespace EdsMediaArchiver.Services.Processors;

public interface IArchiveProcessor
{
    Task<ProcessingResult> ProcessFileAsync(ArchiveRequest request);
}

/// <summary>
/// Orchestrates file processing by delegating to specialized processors
/// based on the user's preferences set on the request.
/// </summary>
public class ArchiveProcessor(
    IExtensionFixProcessor extensionFixProcessor,
    ICompressProcessor compressProcessor,
    IDateProcessor dateProcessor) : IArchiveProcessor
{
    public async Task<ProcessingResult> ProcessFileAsync(ArchiveRequest request)
    {
        try
        {
            if (request.FixExtension)
                extensionFixProcessor.Process(request, request.ActualFileType);

            if (request.Compress)
            {
                var result = await compressProcessor.ProcessAsync(request, request.ActualFileType);
                if (result != null) return result;
            }

            if (request.SetDates)
                return await dateProcessor.ProcessAsync(request, request.ActualFileType);

            // Only extension was fixed — detect via path change
            if (request.NewPath.Absolute != request.OriginalPath.Absolute)
                return new ProcessingResult(request.NewPath.Relative, null, ProcessingStatus.Renamed);

            File.Move(request.OriginalPath.Absolute, request.NewPath.Absolute);

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
