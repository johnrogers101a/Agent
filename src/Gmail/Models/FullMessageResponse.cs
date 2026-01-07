#nullable enable

namespace Gmail;

public record FullMessageResponse(string Id, string ThreadId, string? Snippet, string? InternalDate, FullPayload? Payload);
