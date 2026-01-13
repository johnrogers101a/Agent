namespace Fetch.Models;

/// <summary>
/// Response from extracting page content.
/// </summary>
public sealed record PageContentResponse
{
    /// <summary>
    /// The URL that was fetched.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Whether the fetch was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Page title.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// LLM-optimized markdown content (filtered/pruned).
    /// </summary>
    public string? FitMarkdown { get; init; }

    /// <summary>
    /// Full markdown content (unfiltered).
    /// </summary>
    public string? RawMarkdown { get; init; }

    /// <summary>
    /// Word count of the fit markdown.
    /// </summary>
    public int WordCount { get; init; }

    /// <summary>
    /// Error message if fetch failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Path to error screenshot (only on failure).
    /// </summary>
    public string? ScreenshotPath { get; init; }
}
