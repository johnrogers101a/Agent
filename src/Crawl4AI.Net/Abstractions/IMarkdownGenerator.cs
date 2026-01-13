namespace Crawl4AI.Abstractions;

/// <summary>
/// Interface for generating markdown from HTML content.
/// </summary>
public interface IMarkdownGenerator
{
    /// <summary>
    /// Generates markdown from HTML content.
    /// </summary>
    /// <param name="html">The HTML content to convert.</param>
    /// <param name="baseUrl">Base URL for resolving relative links.</param>
    /// <returns>Markdown generation result with raw and fit markdown.</returns>
    MarkdownResult Generate(string html, string? baseUrl = null);
}
