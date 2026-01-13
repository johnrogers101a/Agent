namespace Crawl4AI.Abstractions;

/// <summary>
/// Result of markdown generation containing raw and filtered versions.
/// </summary>
public sealed record MarkdownResult
{
    /// <summary>
    /// Full unfiltered markdown conversion of the HTML.
    /// </summary>
    public required string RawMarkdown { get; init; }

    /// <summary>
    /// Filtered "fit" markdown with boilerplate removed (null if no filter applied).
    /// </summary>
    public string? FitMarkdown { get; init; }

    /// <summary>
    /// The filtered HTML that produced FitMarkdown (null if no filter applied).
    /// </summary>
    public string? FitHtml { get; init; }

    /// <summary>
    /// Extracted page title.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Markdown-formatted references/citations extracted from links.
    /// </summary>
    public string? ReferencesMarkdown { get; init; }

    /// <summary>
    /// Word count of the fit markdown (or raw if no filter).
    /// </summary>
    public int WordCount => (FitMarkdown ?? RawMarkdown)
        .Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries).Length;
}
