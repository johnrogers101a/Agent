#nullable enable

using System.Text.Json.Serialization;

namespace Gmail;

public class TokenData
{
    [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
    [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
    [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
    public DateTime ExpiresAt { get; set; }
}
