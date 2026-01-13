using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace Crawl4AI.Html;

/// <summary>
/// Utilities for cleaning HTML content before processing.
/// </summary>
public static class HtmlCleaner
{
    /// <summary>
    /// Tags to remove entirely from HTML.
    /// </summary>
    private static readonly HashSet<string> TagsToRemove = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style", "noscript", "iframe", "svg", "canvas",
        "video", "audio", "form", "input", "button", "select", "textarea"
    };

    /// <summary>
    /// Tags to unwrap (remove tag but keep content).
    /// </summary>
    private static readonly HashSet<string> TagsToUnwrap = new(StringComparer.OrdinalIgnoreCase)
    {
        "span", "font", "b", "i", "u", "strong", "em"
    };

    /// <summary>
    /// Cleans HTML by removing scripts, styles, and other non-content elements.
    /// </summary>
    /// <param name="html">Raw HTML to clean.</param>
    /// <returns>Cleaned HTML string.</returns>
    public static string Clean(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return "";

        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        // Remove comments
        foreach (var comment in document.Descendants<IComment>().ToList())
        {
            comment.Remove();
        }

        // Remove unwanted tags
        foreach (var tagName in TagsToRemove)
        {
            foreach (var element in document.QuerySelectorAll(tagName).ToList())
            {
                element.Remove();
            }
        }

        // Remove hidden elements
        foreach (var element in document.QuerySelectorAll("[style*='display:none'], [style*='display: none'], [hidden]").ToList())
        {
            element.Remove();
        }

        // Remove elements with aria-hidden="true"
        foreach (var element in document.QuerySelectorAll("[aria-hidden='true']").ToList())
        {
            element.Remove();
        }

        return document.Body?.InnerHtml ?? document.DocumentElement.OuterHtml;
    }

    /// <summary>
    /// Extracts just the text content from HTML, preserving paragraph breaks.
    /// </summary>
    /// <param name="html">HTML to extract text from.</param>
    /// <returns>Plain text content.</returns>
    public static string ExtractText(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return "";

        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        var body = document.Body ?? document.DocumentElement;
        var text = body.TextContent ?? "";

        // Clean up whitespace
        return string.Join("\n", text
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line)));
    }

    /// <summary>
    /// Extracts the main content area from HTML, removing nav, header, footer, etc.
    /// </summary>
    /// <param name="html">HTML to process.</param>
    /// <returns>HTML of the main content area.</returns>
    public static string ExtractMainContent(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return "";

        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        // Try to find main content element
        var main = document.QuerySelector("main, article, [role='main'], #content, .content, #main, .main");
        
        if (main is not null)
            return main.OuterHtml;

        // Fall back to body with nav/header/footer removed
        var body = document.Body;
        if (body is null)
            return html;

        // Remove non-content elements
        foreach (var selector in new[] { "nav", "header", "footer", "aside", ".sidebar", "#sidebar", ".nav", ".menu" })
        {
            foreach (var element in body.QuerySelectorAll(selector).ToList())
            {
                element.Remove();
            }
        }

        return body.InnerHtml;
    }
}
