namespace SugoBackend.DTOs;

public class CreateReportDto
{
    public int? TargetUserId { get; set; }
    public int? RoomId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
