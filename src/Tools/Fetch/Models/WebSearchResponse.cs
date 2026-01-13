namespace Fetch.Models;

/// <summary>
/// Response from a web search operation.
/// </summary>
public sealed record WebSearchResponse
{
    /// <summary>
    /// The search query that was executed.
    /// </summary>
    public required string Query { get; init; }

    /// <summary>
    /// Search results.
    /// </summary>
    public required IReadOnlyList<SearchResult> Results { get; init; }

    /// <summary>
    /// Total number of results found.
    /// </summary>
    public int TotalResults => Results.Count;
}

/// <summary>
/// A single search result.
/// </summary>
public sealed record SearchResult
{
    /// <summary>
    /// Title of the search result.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// URL of the search result.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Snippet/description of the search result.
    /// </summary>
    public required string Snippet { get; init; }

    /// <summary>
    /// Position in search results (1-based).
    /// </summary>
    public int Position { get; init; }
}
