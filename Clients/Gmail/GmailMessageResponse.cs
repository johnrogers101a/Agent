using System.Text.Json.Serialization;

namespace Agent.Clients.Gmail;

/// <summary>
/// Response from Gmail API messages.get endpoint.
/// </summary>
public class GmailMessageResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("threadId")]
    public string ThreadId { get; set; } = string.Empty;

    [JsonPropertyName("snippet")]
    public string Snippet { get; set; } = string.Empty;

    [JsonPropertyName("internalDate")]
    public string InternalDate { get; set; } = string.Empty;

    [JsonPropertyName("payload")]
    public MessagePayload? Payload { get; set; }
}

public class MessagePayload
{
    [JsonPropertyName("headers")]
    public MessageHeader[] Headers { get; set; } = [];

    [JsonPropertyName("body")]
    public MessageBody? Body { get; set; }

    [JsonPropertyName("parts")]
    public MessagePart[]? Parts { get; set; }

    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = string.Empty;
}

public class MessageHeader
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

public class MessageBody
{
    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("data")]
    public string? Data { get; set; }
}

public class MessagePart
{
    [JsonPropertyName("partId")]
    public string PartId { get; set; } = string.Empty;

    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public MessageBody? Body { get; set; }

    [JsonPropertyName("parts")]
    public MessagePart[]? Parts { get; set; }
}
