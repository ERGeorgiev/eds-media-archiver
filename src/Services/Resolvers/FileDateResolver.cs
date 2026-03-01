using EdsMediaArchiver.Services.FileDateReaders;

namespace EdsMediaArchiver.Services.Resolvers;

// ToDo: Read mmetadata OffsetTime to get "+05:00" or something like that and take it into consideration

public interface IFileDateResolver
{
    DateTimeOffset? ResolveBestDate(string filePath, IEnumerable<MetadataExtractor.Directory> fileDirectories);
}

/// <summary>
/// Determines the best date for a media file.
/// </summary>
public partial class FileDateResolver(
    IOriginalDateReader originalDateReader,
    IFilenameDateReader filenameDateReader,
    IOldestDateReader oldestDateReader) : IFileDateResolver
{
    private readonly IEnumerable<IFileDateReader> _dateReaders = [originalDateReader, filenameDateReader, oldestDateReader];

    public DateTimeOffset? ResolveBestDate(string filePath, IEnumerable<MetadataExtractor.Directory> fileDirectories)
    {
        foreach (var reader in _dateReaders)
        {
            var date = reader.Read(filePath, fileDirectories);
            if (date.HasValue)
                return date;
        }
        return null;
    }
}
