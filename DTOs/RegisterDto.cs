namespace SugoBackend.DTOs;

/// <summary>
/// DTO for user registration
/// </summary>
public class RegisterDto
{
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
}
