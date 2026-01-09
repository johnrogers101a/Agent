#nullable enable

namespace Gmail;

public record FullPart(string? MimeType, BodyData? Body, FullPart[]? Parts);
