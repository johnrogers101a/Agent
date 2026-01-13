using AgentFramework.Attributes;
using Crawl4AI.Abstractions;
using Crawl4AI.Filters;
using Crawl4AI.Markdown;
using Fetch.Models;
using Fetch.Services;
using Microsoft.Extensions.Logging;

namespace Fetch.Tools;

/// <summary>
/// Fetches and extracts the main content from a web page as LLM-optimized markdown.
/// </summary>
public sealed class GetPageContent
{
    private readonly ILogger<GetPageContent> _logger;
    private readonly WebCrawler _crawler;

    public GetPageContent(ILogger<GetPageContent> logger, WebCrawler crawler)
    {
        _logger = logger;
        _crawler = crawler;
    }

    /// <summary>
    /// Fetches a web page and extracts its main content as markdown.
    /// </summary>
    /// <param name="url">The URL of the web page to fetch.</param>
    /// <param name="query">Optional search query for BM25-based content filtering. If provided, returns content most relevant to the query.</param>
    /// <param name="usePruning">Whether to use pruning filter to remove boilerplate (default true). Set false for full content.</param>
    /// <returns>Page content as LLM-optimized markdown.</returns>
    [McpTool]
    public async Task<PageContentResponse> ExecuteAsync(string url, string? query = null, bool usePruning = true)
    {
        _logger.LogInformation("GetPageContent invoked for {Url}, query={Query}, usePruning={UsePruning}", 
            url, query ?? "(none)", usePruning);

        if (string.IsNullOrWhiteSpace(url))
        {
            _logger.LogWarning("GetPageContent invoked with empty URL");
            return new PageContentResponse
            {
                Url = url ?? "",
                IsSuccess = false,
                ErrorMessage = "URL is required"
            };
        }

        // Ensure URL has protocol
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url;
            _logger.LogDebug("Added https:// prefix to URL: {Url}", url);
        }

        // Fetch the page
        var crawlResult = await _crawler.FetchAsync(url);

        if (!crawlResult.IsSuccess)
        {
            _logger.LogWarning("Failed to fetch {Url}: {Error}", url, crawlResult.ErrorMessage);
            return new PageContentResponse
            {
                Url = url,
                IsSuccess = false,
                ErrorMessage = crawlResult.ErrorMessage,
                ScreenshotPath = crawlResult.Screenshot?.FilePath
            };
        }

        _logger.LogDebug("Successfully fetched {Url}, {Length} bytes", url, crawlResult.Html!.Length);

        // Select content filter
        IContentFilter? contentFilter = null;
        
        if (!string.IsNullOrWhiteSpace(query))
        {
            _logger.LogDebug("Using BM25 filter with query: '{Query}'", query);
            contentFilter = new Bm25ContentFilter(new Bm25FilterOptions
            {
                UserQuery = query,
                Threshold = 1.0
            });
        }
        else if (usePruning)
        {
            _logger.LogDebug("Using pruning filter with default threshold");
            contentFilter = new PruningContentFilter(new PruningFilterOptions
            {
                Threshold = 0.48,
                ThresholdType = ThresholdType.Dynamic,
                MinWordThreshold = 5
            });
        }
        else
        {
            _logger.LogDebug("No content filter applied - returning full content");
        }

        // Generate markdown
        var generator = new MarkdownGenerator(contentFilter);
        var result = generator.Generate(crawlResult.Html!, url);

        var markdown = result.FitMarkdown ?? result.RawMarkdown;
        var wordCount = markdown.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries).Length;

        _logger.LogInformation(
            "GetPageContent completed for {Url}: title='{Title}', {WordCount} words, filter={FilterType}",
            url, result.Title, wordCount, contentFilter?.GetType().Name ?? "none");

        return new PageContentResponse
        {
            Url = url,
            IsSuccess = true,
            Title = result.Title,
            FitMarkdown = result.FitMarkdown,
            RawMarkdown = result.RawMarkdown,
            WordCount = wordCount
        };
    }
}
