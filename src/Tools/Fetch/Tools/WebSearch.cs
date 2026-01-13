using AgentFramework.Attributes;
using Fetch.Models;
using Fetch.Services;
using Microsoft.Extensions.Logging;

namespace Fetch.Tools;

/// <summary>
/// Searches the web using DuckDuckGo and returns relevant results.
/// </summary>
public sealed class WebSearch
{
    private readonly ILogger<WebSearch> _logger;
    private readonly SearchClient _searchClient;

    public WebSearch(ILogger<WebSearch> logger, SearchClient searchClient)
    {
        _logger = logger;
        _searchClient = searchClient;
    }

    /// <summary>
    /// Searches the web for information on a given query.
    /// </summary>
    /// <param name="query">The search query (e.g., "weather in Seattle", "latest news on AI").</param>
    /// <param name="maxResults">Maximum number of results to return (default 10, max 20).</param>
    /// <returns>Search results with titles, URLs, and snippets.</returns>
    [McpTool]
    public async Task<WebSearchResponse> ExecuteAsync(string query, int maxResults = 10)
    {
        _logger.LogInformation("WebSearch tool invoked with query: '{Query}', maxResults: {MaxResults}", 
            query, maxResults);

        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogWarning("WebSearch invoked with empty query");
            return new WebSearchResponse
            {
                Query = query ?? "",
                Results = Array.Empty<SearchResult>()
            };
        }

        // Cap max results
        maxResults = Math.Clamp(maxResults, 1, 20);

        var response = await _searchClient.SearchAsync(query, maxResults);

        _logger.LogInformation("WebSearch completed: {Count} results for '{Query}'", 
            response.TotalResults, query);

        return response;
    }
}
