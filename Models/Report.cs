namespace SugoBackend.Models;

public enum ReportStatus
{
    Pending = 0,
    InReview = 1,
    Resolved = 2,
    Rejected = 3
}

public class Report
{
    public int Id { get; set; }
    public int ReporterUserId { get; set; }
    public int? TargetUserId { get; set; }
    public int? RoomId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public ReportStatus Status { get; set; } = ReportStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    public User? ReporterUser { get; set; }
    public User? TargetUser { get; set; }
    public Room? Room { get; set; }
}
