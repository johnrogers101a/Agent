#nullable enable

using System.Text.Json;
using AgentFramework.Attributes;
using Hotmail.Models;
using Microsoft.Extensions.Logging;

namespace Hotmail.Tools;

/// <summary>
/// Searches emails in Hotmail/Outlook.com using Microsoft Graph search.
/// </summary>
public class SearchHotmail
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly TimeZoneInfo s_pacificZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
    private static readonly string[] s_scopes = ["https://graph.microsoft.com/Mail.Read"];

    private readonly HotmailClientFactory _clientFactory;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SearchHotmail> _logger;

    public SearchHotmail(HotmailClientFactory clientFactory, HttpClient httpClient, ILogger<SearchHotmail> logger)
    {
        _clientFactory = clientFactory;
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Searches emails in Hotmail/Outlook.com using full-text search.
    /// Searches across subject, body, and sender. Just use simple keywords like "walmart" or "order confirmation".
    /// Do NOT use Gmail-style syntax like "from:email" - just use plain keywords.
    /// </summary>
    /// <param name="query">Simple search keywords. Examples: "walmart", "amazon order", "receipt", "taco bell"</param>
    /// <param name="maxResults">Maximum number of emails to retrieve (default 20).</param>
    [McpTool]
    public async Task<SearchHotmailResponse> ExecuteAsync(string query, int maxResults = 20)
    {
        _logger.LogInformation("[SearchHotmail] Starting search with Query=\"{Query}\", MaxResults={MaxResults}", query, maxResults);

        string token;
        try
        {
            token = await _clientFactory.GetTokenAsync(s_scopes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SearchHotmail failed: Unable to obtain access token");
            return new SearchHotmailResponse(false, [], Errors.UnableToObtainToken);
        }

        // Clean up the query - remove Gmail-style syntax that doesn't work with Graph API
        var cleanQuery = CleanupQuery(query);
        _logger.LogInformation("[SearchHotmail] Cleaned query: \"{CleanQuery}\" (original: \"{OriginalQuery}\")", cleanQuery, query);

        // Use $search for full-text search across messages
        // Note: $orderby is NOT supported with $search in Microsoft Graph API
        // We fetch more results and sort client-side by date
        var encodedQuery = Uri.EscapeDataString($"\"{cleanQuery}\"");
        var fetchCount = Math.Max(maxResults * 3, 100); // Fetch extra to ensure we get recent ones
        var url = $"{Constants.GraphApiBase}/me/messages?$search={encodedQuery}&$top={fetchCount}&$select=id,subject,from,toRecipients,bodyPreview,receivedDateTime,isRead";
        
        _logger.LogInformation("[SearchHotmail] Request URL: {Url}", url);
        
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("[SearchHotmail] Failed: {StatusCode} {Error}", response.StatusCode, errorBody);
            return new SearchHotmailResponse(false, [], string.Format(Errors.FailedToFetchMessages, response.StatusCode));
        }

        var json = await response.Content.ReadAsStringAsync();
        var listResponse = JsonSerializer.Deserialize<GraphMessageListResponse>(json, s_jsonOptions);
        
        if (listResponse?.Value is null || listResponse.Value.Length == 0)
        {
            _logger.LogInformation("[SearchHotmail] No messages found for query: \"{Query}\"", cleanQuery);
            return new SearchHotmailResponse(true, []);
        }

        _logger.LogInformation("[SearchHotmail] Found {Count} messages for query \"{Query}\", sorting by date", listResponse.Value.Length, cleanQuery);

        // Sort by date descending (newest first) since Graph API returns by relevance when using $search
        var sortedMessages = listResponse.Value
            .OrderByDescending(msg => msg.ReceivedDateTime)
            .Take(maxResults);

        var messages = sortedMessages.Select(msg => new HotmailMessage(
            msg.Id,
            msg.Subject ?? "(No Subject)",
            FormatEmailAddress(msg.From),
            FormatToRecipients(msg.ToRecipients),
            msg.BodyPreview ?? "",
            ConvertToPacific(msg.ReceivedDateTime),
            msg.IsRead
        )).ToList();

        _logger.LogInformation("[SearchHotmail] Returning {Count} messages (sorted by date)", messages.Count);
        return new SearchHotmailResponse(true, messages);
    }

    /// <summary>
    /// Cleans up the query to work with Microsoft Graph's $search.
    /// Removes Gmail-style syntax and extracts just the keywords.
    /// </summary>
    private static string CleanupQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return query;

        // Remove Gmail-style operators that don't work with Graph API
        // from:domain.com -> domain.com
        // subject:text -> text
        // after:date, before:date -> remove entirely
        var cleaned = query;

        // Remove date filters (Graph API doesn't support them in $search)
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\b(after|before):\S+", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Convert from:domain.com to just domain - extract brand name
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\bfrom:(\S+)", m => {
            var domain = m.Groups[1].Value;
            // Extract brand name from domain (e.g., "walmart.com" -> "walmart", "ship-confirm@amazon.com" -> "amazon")
            var atIndex = domain.IndexOf('@');
            if (atIndex >= 0)
                domain = domain.Substring(atIndex + 1);
            var dotIndex = domain.IndexOf('.');
            if (dotIndex >= 0)
                domain = domain.Substring(0, dotIndex);
            return domain;
        }, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Remove subject: prefix, keep the text
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\bsubject:(\S+)", "$1", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Remove OR operators (Graph uses spaces for OR in $search)
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\bOR\b", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Remove AND operators
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\bAND\b", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Clean up multiple spaces
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ").Trim();

        return cleaned;
    }

    private static string FormatEmailAddress(GraphEmailAddress? addr)
    {
        if (addr?.EmailAddress == null)
            return "(Unknown)";
        
        var name = addr.EmailAddress.Name;
        var email = addr.EmailAddress.Address;
        
        if (string.IsNullOrEmpty(name) || name == email)
            return email ?? "(Unknown)";
        
        return $"{name} ({email})";
    }

    private static string FormatToRecipients(GraphEmailAddress[]? recipients)
    {
        if (recipients == null || recipients.Length == 0)
            return "(Unknown)";
        
        return string.Join(", ", recipients.Select(FormatEmailAddress));
    }

    private static DateTime ConvertToPacific(DateTime utcDateTime)
    {
        var utc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, s_pacificZone);
    }
}
