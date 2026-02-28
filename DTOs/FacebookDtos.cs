using System.Text.Json.Serialization;

namespace SugoBackend.DTOs;

public class FacebookDebugTokenResponse
{
    [JsonPropertyName("data")]
    public FacebookDebugTokenData Data { get; set; } = new();
}

public class FacebookDebugTokenData
{
    [JsonPropertyName("app_id")]
    public string AppId { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("application")]
    public string Application { get; set; } = string.Empty;

    [JsonPropertyName("data_access_expires_at")]
    public long DataAccessExpiresAt { get; set; }

    [JsonPropertyName("expires_at")]
    public long ExpiresAt { get; set; }

    [JsonPropertyName("is_valid")]
    public bool IsValid { get; set; }

    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;
}

public class FacebookUserProfileResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}
