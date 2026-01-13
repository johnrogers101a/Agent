namespace Crawl4AI.Models;

/// <summary>
/// Metrics computed for a DOM node during content filtering.
/// </summary>
public sealed record ContentMetrics
{
    /// <summary>
    /// HTML tag name (e.g., "div", "p", "article").
    /// </summary>
    public required string TagName { get; init; }

    /// <summary>
    /// Length of text content (stripped).
    /// </summary>
    public int TextLength { get; init; }

    /// <summary>
    /// Length of raw HTML markup.
    /// </summary>
    public int TagLength { get; init; }

    /// <summary>
    /// Total length of text within anchor tags.
    /// </summary>
    public int LinkTextLength { get; init; }

    /// <summary>
    /// Ratio of text to markup (0-1). Higher = more text-dense.
    /// </summary>
    public double TextDensity => TagLength > 0 ? (double)TextLength / TagLength : 0;

    /// <summary>
    /// Ratio of link text to total text (0-1). Higher = more link-heavy.
    /// </summary>
    public double LinkDensity => TextLength > 0 ? (double)LinkTextLength / TextLength : 1;

    /// <summary>
    /// Word count in the text content.
    /// </summary>
    public int WordCount => TextLength > 0 
        ? Text?.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries).Length ?? 0 
        : 0;

    /// <summary>
    /// The actual text content.
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// CSS class names on the element.
    /// </summary>
    public string? ClassName { get; init; }

    /// <summary>
    /// Element ID attribute.
    /// </summary>
    public string? Id { get; init; }
}
