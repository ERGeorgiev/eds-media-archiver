namespace PersonalMediaArchiver.Models;

public enum ProcessingStatus
{
    Fixed,
    Converted,
    Skipped,
    Error
}

public record ProcessingResult(
    string RelativePath,
    DateTime? DateAssigned,
    ProcessingStatus Status,
    string? ErrorMessage = null);
