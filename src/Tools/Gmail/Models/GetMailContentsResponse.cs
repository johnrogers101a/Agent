#nullable enable

namespace Gmail.Models;

public record GetMailContentsResponse(bool Success, EmailDetail? Email, string? Error = null);

// Renamed version for consistency with tool name
public record GetGmailContentsResponse(bool Success, EmailDetail? Email, string? Error = null);
