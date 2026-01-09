#nullable enable

namespace Gmail;

public record FullPayload(MessageHeader[]? Headers = null, BodyData? Body = null, FullPart[]? Parts = null);
