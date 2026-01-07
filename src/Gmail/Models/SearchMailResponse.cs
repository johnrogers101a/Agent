#nullable enable

namespace Gmail.Models;

public record SearchMailResponse(bool Success, List<EmailMessage> Messages, string? Error = null);
