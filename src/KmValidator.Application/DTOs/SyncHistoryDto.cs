namespace KmValidator.Application.DTOs;

public class SyncHistoryRequestDto
{
    public string DeviceName { get; set; } = "Xiaomi Mi Band 10";
    public string? FolderPath { get; set; }
}

public class SyncHistoryResponseDto
{
    public int TotalFound { get; set; }
    public int SyncedCount { get; set; }
    public int NewCount { get; set; }
    public int AlreadyExistingCount { get; set; }
    public List<ActivitySummaryDto> Activities { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
