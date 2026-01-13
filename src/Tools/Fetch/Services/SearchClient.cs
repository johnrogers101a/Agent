using System.Text.RegularExpressions;
using System.Web;
using AngleSharp.Html.Parser;
using Fetch.Models;
using Microsoft.Extensions.Logging;

namespace Fetch.Services;

/// <summary>
/// Search client using DuckDuckGo HTML interface (no API key required).
/// </summary>
public sealed partial class SearchClient
{
    private readonly ILogger<SearchClient> _logger;
    private readonly WebCrawler _crawler;

    public SearchClient(ILogger<SearchClient> logger, WebCrawler crawler)
    {
        _logger = logger;
        _crawler = crawler;
    }

    /// <summary>
    /// Searches the web using DuckDuckGo.
    /// </summary>
    /// <param name="query">Search query.</param>
    /// <param name="maxResults">Maximum results to return (default 10).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Search results.</returns>
    public async Task<WebSearchResponse> SearchAsync(string query, int maxResults = 10, CancellationToken ct = default)
    {
        _logger.LogInformation("Searching DuckDuckGo for: '{Query}' (max {MaxResults} results)", query, maxResults);

        var encodedQuery = HttpUtility.UrlEncode(query);
        var searchUrl = $"{Urls.DuckDuckGoHtml}?q={encodedQuery}";

        _logger.LogDebug("Search URL: {Url}", searchUrl);

        var crawlResult = await _crawler.FetchAsync(searchUrl, ct);

        if (!crawlResult.IsSuccess)
        {
            _logger.LogWarning("Search failed for '{Query}': {Error}", query, crawlResult.ErrorMessage);
            return new WebSearchResponse
            {
                Query = query,
                Results = Array.Empty<SearchResult>()
            };
        }

        var results = ParseSearchResults(crawlResult.Html!, maxResults);

        _logger.LogInformation("Search for '{Query}' returned {Count} results", query, results.Count);

        return new WebSearchResponse
        {
            Query = query,
            Results = results
        };
    }

    private List<SearchResult> ParseSearchResults(string html, int maxResults)
    {
        var results = new List<SearchResult>();
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        // DuckDuckGo HTML results are in divs with class "result"
        var resultElements = document.QuerySelectorAll(".result, .results_links_deep");

        _logger.LogDebug("Found {Count} result elements in HTML", resultElements.Length);

        var position = 1;
        foreach (var element in resultElements)
        {
            if (position > maxResults)
                break;

            try
            {
                // Get title and URL from the result link
                var linkElement = element.QuerySelector(".result__a, a.result__a");
                if (linkElement is null)
                {
                    _logger.LogDebug("Skipping result element - no link found");
                    continue;
                }

                var title = linkElement.TextContent?.Trim();
                var href = linkElement.GetAttribute("href");

                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(href))
                {
                    _logger.LogDebug("Skipping result - missing title or href");
                    continue;
                }

                // Extract actual URL from DuckDuckGo redirect URL
                var url = ExtractActualUrl(href);
                if (string.IsNullOrWhiteSpace(url))
                {
                    _logger.LogDebug("Skipping result - could not extract URL from {Href}", href);
                    continue;
                }

                // Get snippet
                var snippetElement = element.QuerySelector(".result__snippet, .result__body");
                var snippet = snippetElement?.TextContent?.Trim() ?? "";

                results.Add(new SearchResult
                {
                    Title = title,
                    Url = url,
                    Snippet = snippet,
                    Position = position
                });

                _logger.LogDebug("Result {Position}: {Title} - {Url}", position, title, url);
                position++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing search result element");
            }
        }

        return results;
    }

    private string? ExtractActualUrl(string href)
    {
        // DuckDuckGo often uses redirect URLs like //duckduckgo.com/l/?uddg=...
        if (href.Contains("uddg="))
        {
            var match = UddgRegex().Match(href);
            if (match.Success)
            {
                return HttpUtility.UrlDecode(match.Groups[1].Value);
            }
        }

        // Direct URL
        if (href.StartsWith("http"))
        {
            return href;
        }

        // Protocol-relative URL
        if (href.StartsWith("//"))
        {
            return "https:" + href;
        }

        return null;
    }

    [GeneratedRegex(@"uddg=([^&]+)")]
    private static partial Regex UddgRegex();
}
