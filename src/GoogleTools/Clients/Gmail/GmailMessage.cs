namespace GoogleTools.Clients.Gmail;

/// <summary>
/// Represents a simplified Gmail message summary.
/// </summary>
public record GmailMessage(
    string Id,
    string ThreadId,
    string Snippet,
    string From,
    string To,
    string Subject,
    DateTime Date
);
