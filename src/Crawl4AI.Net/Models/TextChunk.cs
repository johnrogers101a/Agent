namespace Crawl4AI.Models;

/// <summary>
/// Represents an extracted text chunk from HTML with metadata.
/// </summary>
public sealed record TextChunk
{
    /// <summary>
    /// Index position in the original document order.
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// The extracted text content.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// The HTML tag type (e.g., "p", "h1", "article").
    /// </summary>
    public required string TagType { get; init; }

    /// <summary>
    /// The cleaned HTML fragment.
    /// </summary>
    public required string Html { get; init; }

    /// <summary>
    /// Relevance score from filtering algorithm.
    /// </summary>
    public double Score { get; init; }
}
