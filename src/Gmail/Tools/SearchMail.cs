#nullable enable

using AgentFramework.Attributes;
using AgentFramework.Http;
using Gmail.Models;
using Microsoft.Extensions.Logging;

namespace Gmail.Tools;

/// <summary>
/// Searches Gmail using Gmail's search syntax.
/// </summary>
public class SearchMail : ClientBase
{
    private readonly AuthService _auth;
    private readonly ILogger<SearchMail> _logger;

    public SearchMail(HttpClient httpClient, AuthService auth, ILogger<SearchMail> logger) 
        : base(httpClient, Urls.GmailApi)
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
    public async Task<SearchMailResponse> ExecuteAsync(string query, int maxResults = 20)
    {
        _logger.LogTrace("SearchMail starting with Query={Query}, MaxResults={MaxResults}", query, maxResults);

        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogWarning("SearchMail failed: Query is required");
            return new SearchMailResponse(false, [], Errors.QueryRequired);
        }

        var token = await _auth.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("SearchMail failed: Unable to obtain access token");
            return new SearchMailResponse(false, [], Errors.UnableToObtainToken);
        }
        _logger.LogTrace("SearchMail obtained access token");

        var listRequest = Get("messages")
            .AddQueryParam("maxResults", maxResults.ToString())
            .AddQueryParam("q", query)
            .SetBearerAuth(token);

        _logger.LogTrace("SearchMail fetching message list from {Url}", listRequest.BuildUrl());
        var listResponse = await SendAsync<MessageListResponse>(listRequest);
        
        if (!listResponse.IsSuccess)
        {
            _logger.LogWarning("SearchMail failed to fetch message list: {StatusCode} {Error}", listResponse.StatusCode, listResponse.Error);
            return new SearchMailResponse(false, [], string.Format(Errors.FailedToSearchMessages, listResponse.StatusCode));
        }

        if (listResponse.Data?.Messages is null)
        {
            _logger.LogTrace("SearchMail found no matching messages");
            return new SearchMailResponse(true, []);
        }

        _logger.LogTrace("SearchMail found {Count} matching message IDs", listResponse.Data.Messages.Length);

        var messages = new List<EmailMessage>();
        foreach (var msg in listResponse.Data.Messages)
        {
            _logger.LogTrace("SearchMail fetching message details for {MessageId}", msg.Id);
            var detail = await GetMessageAsync(msg.Id, token);
            if (detail is not null)
            {
                messages.Add(detail);
                _logger.LogTrace("SearchMail retrieved message: Subject={Subject}", detail.Subject);
            }
            else
            {
                _logger.LogWarning("SearchMail failed to retrieve details for message {MessageId}", msg.Id);
            }
        }

        _logger.LogTrace("SearchMail completed successfully with {Count} messages", messages.Count);
        return new SearchMailResponse(true, messages);
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
            GetHeader(headers, "From"),
            GetHeader(headers, "To"),
            GetHeader(headers, "Subject"),
            ParseDate(r.InternalDate));
    }

    private static string GetHeader(MessageHeader[] headers, string name) =>
        headers.FirstOrDefault(h => h.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value ?? "";

    private static DateTime ParseDate(string? internalDate) =>
        long.TryParse(internalDate, out var ms) ? DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime : DateTime.MinValue;
}
