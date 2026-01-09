#nullable enable

namespace Gmail;

public record EmailMessage(string Id, string ThreadId, string Snippet, string From, string To, string Subject, DateTime Date);
