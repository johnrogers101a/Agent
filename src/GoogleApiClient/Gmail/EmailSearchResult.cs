namespace GoogleApiClient.Gmail;

/// <summary>
/// Email search/list result for tool responses.
/// </summary>
public record EmailSearchResult(
    int Count,
    string? Query,
    List<GmailMessage> Emails);
