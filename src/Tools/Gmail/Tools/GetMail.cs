#nullable enable

using AgentFramework.Attributes;
using AgentFramework.Http;
using Gmail.Models;
using Microsoft.Extensions.Logging;

namespace Gmail.Tools;

/// <summary>
/// Retrieves recent emails from Gmail inbox.
/// </summary>
public class GetMail : ClientBase
{
    private readonly AuthService _auth;
    private readonly ILogger<GetMail> _logger;

    public GetMail(HttpClient httpClient, AuthService auth, ILogger<GetMail> logger) 
        : base(httpClient, Urls.GmailApi)
    {
        _auth = auth;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves recent emails from the user's Gmail inbox. Returns a list of email summaries including sender, subject, and snippet.
    /// </summary>
    /// <param name="maxResults">Maximum number of emails to retrieve (default 20).</param>
    [McpTool]
    public async Task<GetMailResponse> ExecuteAsync(int maxResults = 20)
    {
        _logger.LogTrace("GetMail starting with MaxResults={MaxResults}", maxResults);

        var token = await _auth.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("GetMail failed: Unable to obtain access token");
            return new GetMailResponse(false, [], Errors.UnableToObtainToken);
        }
        _logger.LogTrace("GetMail obtained access token");

        var listRequest = Get("messages")
            .AddQueryParam("maxResults", maxResults.ToString())
            .SetBearerAuth(token);

        _logger.LogTrace("GetMail fetching message list from {Url}", listRequest.BuildUrl());
        var listResponse = await SendAsync<MessageListResponse>(listRequest);
        
        if (!listResponse.IsSuccess)
        {
            _logger.LogWarning("GetMail failed to fetch message list: {StatusCode} {Error}", listResponse.StatusCode, listResponse.Error);
            return new GetMailResponse(false, [], string.Format(Errors.FailedToFetchMessages, listResponse.StatusCode));
        }

        if (listResponse.Data?.Messages is null)
        {
            _logger.LogTrace("GetMail found no messages");
            return new GetMailResponse(true, []);
        }

        _logger.LogTrace("GetMail found {Count} message IDs", listResponse.Data.Messages.Length);

        var messages = new List<EmailMessage>();
        foreach (var msg in listResponse.Data.Messages)
        {
            _logger.LogTrace("GetMail fetching message details for {MessageId}", msg.Id);
            var detail = await GetMessageAsync(msg.Id, token);
            if (detail is not null)
            {
                messages.Add(detail);
                _logger.LogTrace("GetMail retrieved message: Subject={Subject}", detail.Subject);
            }
            else
            {
                _logger.LogWarning("GetMail failed to retrieve details for message {MessageId}", msg.Id);
            }
        }

        _logger.LogTrace("GetMail completed successfully with {Count} messages", messages.Count);
        return new GetMailResponse(true, messages);
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

        var headers = response.Data.Payload?.Headers ?? [];
        return new EmailMessage(
            response.Data.Id,
            response.Data.ThreadId,
            response.Data.Snippet ?? "",
            GetHeader(headers, "From"),
            GetHeader(headers, "To"),
            GetHeader(headers, "Subject"),
            ParseDate(response.Data.InternalDate));
    }

    private static string GetHeader(MessageHeader[] headers, string name) =>
        headers.FirstOrDefault(h => h.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value ?? "";

    private static DateTime ParseDate(string? internalDate) =>
        long.TryParse(internalDate, out var ms) ? DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime : DateTime.MinValue;
}
