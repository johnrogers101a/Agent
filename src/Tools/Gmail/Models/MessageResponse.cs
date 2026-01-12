#nullable enable

namespace Gmail;

public record MessageResponse(string Id, string ThreadId, string? Snippet, string? InternalDate, MessagePayload? Payload, string[]? LabelIds);
