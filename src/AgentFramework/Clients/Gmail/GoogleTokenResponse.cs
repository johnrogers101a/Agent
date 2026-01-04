using System.Text.Json.Serialization;

namespace Agent.Clients.Gmail;

/// <summary>
/// Response from Google OAuth2 token endpoint.
/// </summary>
public class GoogleTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;

    // Computed property for expiration tracking
    [JsonPropertyName("expires_at")]
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Error response from Google OAuth2.
/// </summary>
public class GoogleTokenError
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }
}
