namespace Crawl4AI.Abstractions;

/// <summary>
/// Base interface for content filtering strategies that extract relevant content from HTML.
/// </summary>
public interface IContentFilter
{
    /// <summary>
    /// Filters HTML content and returns a list of relevant text chunks.
    /// </summary>
    /// <param name="html">The HTML content to filter.</param>
    /// <param name="minWordThreshold">Minimum words required for a chunk to be included.</param>
    /// <returns>List of filtered text chunks (HTML fragments).</returns>
    IReadOnlyList<string> FilterContent(string html, int? minWordThreshold = null);
}
