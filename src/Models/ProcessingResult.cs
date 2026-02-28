namespace EdsMediaArchiver.Models;

public enum ProcessingStatus
{
    Fixed,
    Converted,
    Renamed,
    Skipped,
    Error
}

public record ProcessingResult(
    string RelativePath,
    DateTimeOffset? DateAssigned,
    ProcessingStatus Status,
    string? ErrorMessage = null);
