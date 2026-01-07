#nullable enable

namespace Gmail.Models;

public record SearchMailRequest(string Query, int MaxResults = 20);
