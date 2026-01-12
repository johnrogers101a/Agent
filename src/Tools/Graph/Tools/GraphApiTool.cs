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
    /// <param name="path">API path starting with /v1.0 or /beta (e.g., /v1.0/me, /v1.0/me/messages)</param>
    /// <param name="body">Optional request body as JSON string</param>
    [McpTool]
    public async Task<string> ExecuteAsync(string method, string path, string? body = null)
    {
        try
        {
            _logger.LogTrace("GraphApiTool starting: {Method} {Path}", method, path);

            var token = await _clientFactory.GetTokenAsync(_scopes);
            var url = path.StartsWith("http") ? path : $"https://graph.microsoft.com{path}";
            
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
}
