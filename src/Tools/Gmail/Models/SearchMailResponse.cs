#nullable enable

namespace Gmail.Models;

public record SearchMailResponse(bool Success, List<EmailMessage> Messages, string? Error = null);

// Renamed version for consistency with tool name
public record SearchGmailResponse(bool Success, List<EmailMessage> Messages, string? Error = null);
