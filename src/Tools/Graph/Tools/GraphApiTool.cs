using System.Text;
using AgentFramework.Attributes;
using Microsoft.Extensions.Logging;

namespace Graph.Tools;

/// <summary>
/// Microsoft Graph API tool for calling user, mail, calendar, files, and organizational data endpoints.
/// </summary>
public class GraphApiTool
{
    private readonly GraphClientFactory _clientFactory;
    private readonly HttpClient _httpClient;
    private readonly ILogger<GraphApiTool> _logger;
    private readonly string[] _scopes;

    public GraphApiTool(GraphClientFactory clientFactory, HttpClient httpClient, ILogger<GraphApiTool> logger)
    {
        _clientFactory = clientFactory;
        _httpClient = httpClient;
        _logger = logger;
        _scopes = new[] { "https://graph.microsoft.com/User.Read" };
    }

    /// <summary>
    /// Calls Microsoft Graph API for user, mail, calendar, files, and organizational data.
    /// </summary>
    /// <param name="method">HTTP method (GET, POST, PUT, PATCH, DELETE)</param>
    /// <param name="path">API path (e.g., /me, /me/messages). Version prefix /v1.0 is added automatically if not specified.</param>
    /// <param name="body">Optional request body as JSON string</param>
    [McpTool]
    public async Task<string> ExecuteAsync(string method, string path, string? body = null)
    {
        try
        {
            _logger.LogTrace("GraphApiTool starting: {Method} {Path}", method, path);

            var token = await _clientFactory.GetTokenAsync(_scopes);
            
            // Normalize the path - add /v1.0 prefix if not already present
            var normalizedPath = NormalizePath(path);
            var url = normalizedPath.StartsWith("http") ? normalizedPath : $"https://graph.microsoft.com{normalizedPath}";
            
            return await ExecuteRequestAsync(method, url, body, token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GraphApiTool failed");
            return $"Error: {ex.Message}";
        }
    }

    private async Task<string> ExecuteRequestAsync(string method, string url, string? body, string token)
    {
        _logger.LogTrace("Executing {Method} request to {Url}", method, url);
        
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        if (!string.IsNullOrEmpty(body))
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();
        
        _logger.LogTrace("Response status: {StatusCode}", (int)response.StatusCode);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Request failed: {StatusCode} {Response}", (int)response.StatusCode, responseBody);
            return $"Error {(int)response.StatusCode} ({response.ReasonPhrase}): {responseBody}";
        }

        return responseBody;
    }

    private static string NormalizePath(string path)
    {
        // If it's a full URL, return as-is
        if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return path;

        // Ensure path starts with /
        if (!path.StartsWith('/'))
            path = "/" + path;

        // If path already has version prefix, return as-is
        if (path.StartsWith("/v1.0", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/beta", StringComparison.OrdinalIgnoreCase))
            return path;

        // Add /v1.0 prefix
        return "/v1.0" + path;
    }
}
