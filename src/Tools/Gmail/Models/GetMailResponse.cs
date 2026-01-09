#nullable enable

namespace Gmail.Models;

public record GetMailResponse(bool Success, List<EmailMessage> Messages, string? Error = null);
