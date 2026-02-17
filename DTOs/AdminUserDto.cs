namespace SugoBackend.DTOs;

public class AdminUserDto
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsBanned { get; set; }
}
