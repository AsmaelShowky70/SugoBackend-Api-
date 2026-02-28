namespace SugoBackend.DTOs;

public class SocialLoginDto
{
    public string Provider { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
}
