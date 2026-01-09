#nullable enable

namespace Gmail;

public record EmailDetail(string Id, string ThreadId, string Snippet, string From, string To, string Subject, DateTime Date, string Body);
