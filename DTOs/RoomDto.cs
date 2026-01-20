namespace SugoBackend.DTOs;

/// <summary>
/// DTO for creating/returning room information
/// </summary>
public class RoomDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
