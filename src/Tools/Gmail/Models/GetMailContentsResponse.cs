#nullable enable

namespace Gmail.Models;

public record GetMailContentsResponse(bool Success, EmailDetail? Email, string? Error = null);
