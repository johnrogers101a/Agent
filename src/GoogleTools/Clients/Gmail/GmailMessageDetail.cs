namespace GoogleTools.Clients.Gmail;

/// <summary>
/// Represents a full Gmail message with body content.
/// </summary>
public record GmailMessageDetail(
    string Id,
    string ThreadId,
    string Snippet,
    string From,
    string To,
    string Subject,
    DateTime Date,
    string Body
);
