namespace SugoBackend.DTOs;

/// <summary>
/// DTO for login response containing JWT token
/// </summary>
public class LoginResponseDto
{
    public int UserId { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string Token { get; set; }
}
