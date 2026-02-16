using SugoBackend.Models;

namespace SugoBackend.DTOs;

public class ReportDto
{
    public int Id { get; set; }
    public int ReporterUserId { get; set; }
    public int? TargetUserId { get; set; }
    public int? RoomId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public ReportStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
