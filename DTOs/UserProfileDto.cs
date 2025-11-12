namespace SugoBackend.DTOs;

/// <summary>
/// DTO for user profile information
/// </summary>
public class UserProfileDto
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public DateTime CreatedAt { get; set; }
}
