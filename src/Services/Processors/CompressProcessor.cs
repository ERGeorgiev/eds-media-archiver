using EdsMediaArchiver.Services.Compressors;
using EdsMediaArchiver.Services.Logging;

namespace EdsMediaArchiver.Services.Processors;

public interface ICompressProcessor
{
    /// <summary>
    /// Fixes the file extension and/or compresses the file based on request flags.
    /// Returns null if no action was taken.
    /// </summary>
    Task<string> ProcessAsync(string sourcePath, string outputDirectory, string fileType, CompressorMode compressorMode);
}

public class CompressProcessor(IEnumerable<IMediaCompressor> compressors, IProcessLogger processLogger) : ICompressProcessor
{
    public async Task<string> ProcessAsync(string sourcePath, string outputDirectory, string fileType, CompressorMode compressorMode)
    {
        var compressor = compressors.FirstOrDefault(c => c.IsSupported(fileType));
        if (compressor != null)
        {
            var outputPath = await compressor.CompressAsync(sourcePath, outputDirectory, compressorMode);

            // Delete the original file (compressor creates a new one)
            if (string.Equals(sourcePath, outputPath, StringComparison.OrdinalIgnoreCase))
            {
                if (compressorMode == CompressorMode.Convert)
                {
                    processLogger.Log(IProcessLogger.Operation.Convert, IProcessLogger.Result.Skip, sourcePath, $"Not necessary.");
                }
                else
                {
                    processLogger.Log(IProcessLogger.Operation.Compress, IProcessLogger.Result.Success, sourcePath, "");
                }
                return outputPath;
            }

            File.Delete(sourcePath);
            processLogger.Log(IProcessLogger.Operation.Compress, IProcessLogger.Result.Success, sourcePath, $"Compressed To: {outputPath}");
            return outputPath;
        }
        return sourcePath;
    }
}
