#nullable enable

using System.Text;
using AgentFramework.Attributes;
using AgentFramework.Http;
using Gmail.Models;
using Microsoft.Extensions.Logging;

namespace Gmail.Tools;

/// <summary>
/// Retrieves full email content by message ID.
/// </summary>
public class GetMailContents : ClientBase
{
    private readonly AuthService _auth;
    private readonly ILogger<GetMailContents> _logger;

    public GetMailContents(HttpClient httpClient, AuthService auth, ILogger<GetMailContents> logger) 
        : base(httpClient, Urls.GmailApi)
    {
        _auth = auth;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the full content of a specific email by its message ID. Use this after GetMail or SearchMail to read the complete email body.
    /// </summary>
    /// <param name="messageId">The unique Gmail message ID obtained from GetMail or SearchMail results.</param>
    [McpTool]
    public async Task<GetMailContentsResponse> ExecuteAsync(string messageId)
    {
        _logger.LogTrace("GetMailContents starting for MessageId={MessageId}", messageId);

        if (string.IsNullOrWhiteSpace(messageId))
        {
            _logger.LogWarning("GetMailContents failed: MessageId is required");
            return new GetMailContentsResponse(false, null, Errors.MessageIdRequired);
        }

        var token = await _auth.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("GetMailContents failed: Unable to obtain access token");
            return new GetMailContentsResponse(false, null, Errors.UnableToObtainToken);
        }
        _logger.LogTrace("GetMailContents obtained access token");

        var httpRequest = Get($"messages/{messageId}")
            .AddQueryParam("format", "full")
            .SetBearerAuth(token);

        _logger.LogTrace("GetMailContents fetching full message from {Url}", httpRequest.BuildUrl());
        var response = await SendAsync<FullMessageResponse>(httpRequest);
        
        if (!response.IsSuccess)
        {
            _logger.LogWarning("GetMailContents failed: {StatusCode} {Error}", response.StatusCode, response.Error);
            return new GetMailContentsResponse(false, null, string.Format(Errors.FailedToFetchMessage, response.StatusCode));
        }

        if (response.Data is null)
        {
            _logger.LogWarning("GetMailContents returned no data for MessageId={MessageId}", messageId);
            return new GetMailContentsResponse(false, null, Errors.MessageNotFound);
        }

        var data = response.Data;
        var headers = data.Payload?.Headers ?? [];
        var body = ExtractBody(data.Payload);
        
        _logger.LogTrace("GetMailContents extracted body with {Length} characters", body.Length);

        var email = new EmailDetail(
            data.Id,
            data.ThreadId,
            data.Snippet ?? "",
            GetHeader(headers, "From"),
            GetHeader(headers, "To"),
            GetHeader(headers, "Subject"),
            ParseDate(data.InternalDate),
            body);

        _logger.LogTrace("GetMailContents completed successfully: Subject={Subject}", email.Subject);
        return new GetMailContentsResponse(true, email);
    }

    private static string GetHeader(MessageHeader[] headers, string name) =>
        headers.FirstOrDefault(h => h.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value ?? "";

    private static DateTime ParseDate(string? internalDate) =>
        long.TryParse(internalDate, out var ms) ? DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime : DateTime.MinValue;

    private static string ExtractBody(FullPayload? payload)
    {
        if (payload is null) return "";
        
        if (payload.Body?.Data is not null)
            return DecodeBase64Url(payload.Body.Data);

        if (payload.Parts is null) return "";

        var textPart = payload.Parts.FirstOrDefault(p => p.MimeType == "text/plain");
        if (textPart?.Body?.Data is not null)
            return DecodeBase64Url(textPart.Body.Data);

        var htmlPart = payload.Parts.FirstOrDefault(p => p.MimeType == "text/html");
        if (htmlPart?.Body?.Data is not null)
            return DecodeBase64Url(htmlPart.Body.Data);

        foreach (var part in payload.Parts)
        {
            if (part.Parts is not null)
            {
                var nested = ExtractBody(new FullPayload { Parts = part.Parts });
                if (!string.IsNullOrEmpty(nested)) return nested;
            }
        }

        return "";
    }

    private static string DecodeBase64Url(string data)
    {
        try
        {
            var base64 = data.Replace('-', '+').Replace('_', '/');
            var padding = base64.Length % 4;
            if (padding > 0) base64 += new string('=', 4 - padding);
            return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        }
        catch { return ""; }
    }
}
