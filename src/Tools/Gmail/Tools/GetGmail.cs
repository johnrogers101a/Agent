#nullable enable

using System.Text.Json;
using AgentFramework.Attributes;
using AgentFramework.Http;
using Gmail.Models;
using Microsoft.Extensions.Logging;

namespace Gmail.Tools;

/// <summary>
/// Retrieves recent emails from Gmail inbox.
/// </summary>
public class GetGmail : ClientBase
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly AuthService _auth;
    private readonly ILogger<GetGmail> _logger;

    public GetGmail(HttpClient httpClient, AuthService auth, ILogger<GetGmail> logger) 
        : base(httpClient, Urls.GmailApi, s_jsonOptions)
    {
        _auth = auth;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves recent emails from the user's Gmail inbox. Returns a list of email summaries including sender, subject, and snippet.
    /// </summary>
    /// <param name="maxResults">Maximum number of emails to retrieve (default 20).</param>
    [McpTool]
    public async Task<GetGmailResponse> ExecuteAsync(int maxResults = 20)
    {
        _logger.LogTrace("GetGmail starting with MaxResults={MaxResults}", maxResults);

        var token = await _auth.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("GetGmail failed: Unable to obtain access token");
            return new GetGmailResponse(false, [], Errors.UnableToObtainToken);
        }
        _logger.LogTrace("GetGmail obtained access token");

        // Get user's email to filter out sent messages
        var userEmail = await _auth.GetUserEmailAsync();

        var listRequest = Get("messages")
            .AddQueryParam("maxResults", maxResults.ToString())
            .AddQueryParam("labelIds", "INBOX")
            .SetBearerAuth(token);

        _logger.LogTrace("GetGmail fetching message list from {Url}", listRequest.BuildUrl());
        var listResponse = await SendAsync<MessageListResponse>(listRequest);
        
        if (!listResponse.IsSuccess)
        {
            _logger.LogWarning("GetGmail failed to fetch message list: {StatusCode} {Error}", listResponse.StatusCode, listResponse.Error);
            return new GetGmailResponse(false, [], string.Format(Errors.FailedToFetchMessages, listResponse.StatusCode));
        }

        if (listResponse.Data?.Messages is null)
        {
            _logger.LogTrace("GetGmail found no messages");
            return new GetGmailResponse(true, []);
        }

        _logger.LogTrace("GetGmail found {Count} message IDs", listResponse.Data.Messages.Length);

        var messages = new List<EmailMessage>();
        foreach (var msg in listResponse.Data.Messages)
        {
            _logger.LogTrace("GetGmail fetching message details for {MessageId}", msg.Id);
            var detail = await GetMessageAsync(msg.Id, token);
            if (detail is not null)
            {
                // Filter out messages sent by the user (replies in inbox threads)
                if (userEmail is not null && detail.From.Contains(userEmail, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogTrace("GetGmail skipping message from self: {From}", detail.From);
                    continue;
                }
                
                messages.Add(detail);
                _logger.LogTrace("GetGmail retrieved message: Subject={Subject}", detail.Subject);
            }
            else
            {
                _logger.LogWarning("GetGmail failed to retrieve details for message {MessageId}", msg.Id);
            }
        }

        _logger.LogTrace("GetGmail completed successfully with {Count} messages", messages.Count);
        return new GetGmailResponse(true, messages);
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
            FormatEmailAddress(GetHeader(headers, "From")),
            FormatEmailAddress(GetHeader(headers, "To")),
            GetHeader(headers, "Subject"),
            ParseDate(response.Data.InternalDate));
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
