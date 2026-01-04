using System.Text.Json.Serialization;

namespace Agent.Clients.Gmail;

/// <summary>
/// Response from Gmail API messages.list endpoint.
/// </summary>
public class GmailListResponse
{
    [JsonPropertyName("messages")]
    public GmailMessageRef[] Messages { get; set; } = [];

    [JsonPropertyName("nextPageToken")]
    public string? NextPageToken { get; set; }

    [JsonPropertyName("resultSizeEstimate")]
    public int ResultSizeEstimate { get; set; }
}

/// <summary>
/// Minimal message reference returned by list endpoint.
/// </summary>
public class GmailMessageRef
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("threadId")]
    public string ThreadId { get; set; } = string.Empty;
}
