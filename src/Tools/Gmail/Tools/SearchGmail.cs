#nullable enable

using System.Text.Json;
using AgentFramework.Attributes;
using AgentFramework.Http;
using Gmail.Models;
using Microsoft.Extensions.Logging;

namespace Gmail.Tools;

/// <summary>
/// Searches Gmail using Gmail's search syntax.
/// </summary>
public class SearchGmail : ClientBase
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly AuthService _auth;
    private readonly ILogger<SearchGmail> _logger;

    public SearchGmail(HttpClient httpClient, AuthService auth, ILogger<SearchGmail> logger) 
        : base(httpClient, Urls.GmailApi, s_jsonOptions)
    {
        _auth = auth;
        _logger = logger;
    }

    /// <summary>
    /// Searches Gmail using Gmail's search syntax. Supports queries like 'from:user@example.com', 'subject:meeting', 'is:unread', 'has:attachment', and date filters like 'after:2024/01/01'.
    /// </summary>
    /// <param name="query">Gmail search query using Gmail's search operators.</param>
    /// <param name="maxResults">Maximum number of emails to retrieve (default 20).</param>
    [McpTool]
    public async Task<SearchGmailResponse> ExecuteAsync(string query, int maxResults = 20)
    {
        _logger.LogTrace("SearchGmail starting with Query={Query}, MaxResults={MaxResults}", query, maxResults);

        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogWarning("SearchGmail failed: Query is required");
            return new SearchGmailResponse(false, [], Errors.QueryRequired);
        }

        var token = await _auth.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("SearchGmail failed: Unable to obtain access token");
            return new SearchGmailResponse(false, [], Errors.UnableToObtainToken);
        }
        _logger.LogTrace("SearchGmail obtained access token");

        var listRequest = Get("messages")
            .AddQueryParam("maxResults", maxResults.ToString())
            .AddQueryParam("q", query)
            .SetBearerAuth(token);

        _logger.LogTrace("SearchGmail fetching message list from {Url}", listRequest.BuildUrl());
        var listResponse = await SendAsync<MessageListResponse>(listRequest);
        
        if (!listResponse.IsSuccess)
        {
            _logger.LogWarning("SearchGmail failed to fetch message list: {StatusCode} {Error}", listResponse.StatusCode, listResponse.Error);
            return new SearchGmailResponse(false, [], string.Format(Errors.FailedToSearchMessages, listResponse.StatusCode));
        }

        if (listResponse.Data?.Messages is null)
        {
            _logger.LogTrace("SearchGmail found no matching messages");
            return new SearchGmailResponse(true, []);
        }

        _logger.LogTrace("SearchGmail found {Count} matching message IDs", listResponse.Data.Messages.Length);

        var messages = new List<EmailMessage>();
        foreach (var msg in listResponse.Data.Messages)
        {
            _logger.LogTrace("SearchGmail fetching message details for {MessageId}", msg.Id);
            var detail = await GetMessageAsync(msg.Id, token);
            if (detail is not null)
            {
                messages.Add(detail);
                _logger.LogTrace("SearchGmail retrieved message: Subject={Subject}", detail.Subject);
            }
            else
            {
                _logger.LogWarning("SearchGmail failed to retrieve details for message {MessageId}", msg.Id);
            }
        }

        _logger.LogTrace("SearchGmail completed successfully with {Count} messages", messages.Count);
        return new SearchGmailResponse(true, messages);
    }

    private async Task<EmailMessage?> GetMessageAsync(string id, string token)
    {
        var request = Get($"messages/{id}")
            .AddQueryParam("format", "metadata")
            .AddQueryParam("metadataHeaders", "From")
            .AddQueryParam("metadataHeaders", "To")
            .AddQueryParam("metadataHeaders", "Subject")
            .SetBearerAuth(token);

        var response = await SendAsync<MessageResponse>(request);
        if (!response.IsSuccess || response.Data is null)
            return null;

        return ToEmailMessage(response.Data);
    }

    private static EmailMessage ToEmailMessage(MessageResponse r)
    {
        var headers = r.Payload?.Headers ?? [];
        return new EmailMessage(
            r.Id,
            r.ThreadId,
            r.Snippet ?? "",
            FormatEmailAddress(GetHeader(headers, "From")),
            FormatEmailAddress(GetHeader(headers, "To")),
            GetHeader(headers, "Subject"),
            ParseDate(r.InternalDate));
    }

    private static string GetHeader(MessageHeader[] headers, string name) =>
        headers.FirstOrDefault(h => h.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value ?? "";

    private static readonly TimeZoneInfo PacificZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");

    private static DateTime ParseDate(string? internalDate)
    {
        if (!long.TryParse(internalDate, out var ms))
            return DateTime.MinValue;
        var utc = DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
        return TimeZoneInfo.ConvertTimeFromUtc(utc, PacificZone);
    }

    /// <summary>
    /// Formats email address from "Name &lt;email@example.com&gt;" to "Name (email@example.com)"
    /// to avoid markdown escaping issues with angle brackets.
    /// </summary>
    private static string FormatEmailAddress(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return raw;

        // Match pattern: "Display Name" <email@example.com> or Display Name <email@example.com>
        var match = System.Text.RegularExpressions.Regex.Match(raw, @"^(?:""?([^""<]+)""?\s*)?<([^>]+)>$");
        if (match.Success)
        {
            var name = match.Groups[1].Value.Trim();
            var email = match.Groups[2].Value.Trim();
            
            if (!string.IsNullOrEmpty(name))
                return $"{name} ({email})";
            return email;
        }
        
        return raw;
    }
}
