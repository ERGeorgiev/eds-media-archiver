namespace EdsMediaArchiver.Models;

public class ArchiveRequest(string rootPath, FileInfo fileInfo, string actualFileType, IReadOnlyList<MetadataExtractor.Directory> directories)
{
    public PathInfo OriginalPath { get; } = new(rootPath, fileInfo.FullName);
    public PathInfo NewPath { get; set; } = new(rootPath, fileInfo.FullName);
    public FileInfo FileInfo { get; } = fileInfo;
    public string ActualFileType { get; } = actualFileType;
    public IReadOnlyList<MetadataExtractor.Directory> MetadataDirectories { get; } = directories;

    public DateTimeOffset? OriginDate { get; set; }
    public bool Compress { get; set; }
    public bool FixExtension { get; set; }
    public bool SetDates { get; set; }
}
