#nullable enable

namespace Gmail.Models;

public record GetMailResponse(bool Success, List<EmailMessage> Messages, string? Error = null);

// Renamed version for consistency with tool name
public record GetGmailResponse(bool Success, List<EmailMessage> Messages, string? Error = null);
