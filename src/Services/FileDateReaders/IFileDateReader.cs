namespace EdsMediaArchiver.Services.FileDateReaders;

public interface IFileDateReader
{
    DateTimeOffset? Read(string filePath, IEnumerable<MetadataExtractor.Directory> fileDirectories);
}
