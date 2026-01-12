#nullable enable

using System.Text.Json;
using System.Text.RegularExpressions;
using AgentFramework.Attributes;
using Hotmail.Models;
using Microsoft.Extensions.Logging;

namespace Hotmail.Tools;

/// <summary>
/// Retrieves the full contents of a specific Hotmail/Outlook.com email by message ID.
/// </summary>
public class GetHotmailContents
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly TimeZoneInfo s_pacificZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
    private static readonly string[] s_scopes = ["https://graph.microsoft.com/Mail.Read"];

    private readonly HotmailClientFactory _clientFactory;
    private readonly HttpClient _httpClient;
    private readonly ILogger<GetHotmailContents> _logger;

    public GetHotmailContents(HotmailClientFactory clientFactory, HttpClient httpClient, ILogger<GetHotmailContents> logger)
    {
        _clientFactory = clientFactory;
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Gets the full contents of a Hotmail/Outlook.com email by its message ID.
    /// </summary>
    /// <param name="messageId">The Hotmail message ID (from GetHotmail or SearchHotmail results).</param>
    [McpTool]
    public async Task<GetHotmailContentsResponse> ExecuteAsync(string messageId)
    {
        _logger.LogTrace("GetHotmailContents starting for MessageId={MessageId}", messageId);

        if (string.IsNullOrWhiteSpace(messageId))
        {
            return new GetHotmailContentsResponse(false, null, "Message ID is required");
        }

        string token;
        try
        {
            token = await _clientFactory.GetTokenAsync(s_scopes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetHotmailContents failed: Unable to obtain access token");
            return new GetHotmailContentsResponse(false, null, Errors.UnableToObtainToken);
        }
        _logger.LogTrace("GetHotmailContents obtained access token");

        // URL-encode the message ID to handle special characters like + and /
        var encodedMessageId = Uri.EscapeDataString(messageId);

        // Fetch the full message including body
        var url = $"{Constants.GraphApiBase}/me/messages/{encodedMessageId}?$select=id,subject,from,toRecipients,body,receivedDateTime,isRead";
        
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        _logger.LogTrace("GetHotmailContents fetching message from Graph API");
        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("GetHotmailContents failed to fetch message: {StatusCode} {Error}", response.StatusCode, errorBody);
            return new GetHotmailContentsResponse(false, null, $"Failed to fetch message: {response.StatusCode}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var msg = JsonSerializer.Deserialize<GraphMessageFull>(json, s_jsonOptions);
        
        if (msg is null)
        {
            _logger.LogWarning("GetHotmailContents: Message not found or could not be parsed");
            return new GetHotmailContentsResponse(false, null, "Message not found");
        }

        // Extract body content - prefer text, fall back to stripping HTML
        var bodyContent = msg.Body?.Content ?? "";
        if (msg.Body?.ContentType?.Equals("html", StringComparison.OrdinalIgnoreCase) == true)
        {
            bodyContent = StripHtml(bodyContent);
        }

        var message = new HotmailMessageFull(
            msg.Id,
            msg.Subject ?? "(No Subject)",
            FormatEmailAddress(msg.From),
            FormatToRecipients(msg.ToRecipients),
            bodyContent,
            ConvertToPacific(msg.ReceivedDateTime),
            msg.IsRead
        );

        _logger.LogTrace("GetHotmailContents completed successfully");
        return new GetHotmailContentsResponse(true, message);
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
            return "";
        
        return string.Join(", ", recipients.Select(FormatEmailAddress));
    }

    private static DateTime ConvertToPacific(DateTime utcDateTime)
    {
        if (utcDateTime.Kind == DateTimeKind.Unspecified)
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, s_pacificZone);
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html))
            return "";

        // Remove style and script blocks
        html = Regex.Replace(html, @"<style[^>]*>[\s\S]*?</style>", "", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
        
        // Replace common block elements with newlines
        html = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"</(p|div|tr|li|h[1-6])>", "\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"</td>", "\t", RegexOptions.IgnoreCase);
        
        // Remove all remaining tags
        html = Regex.Replace(html, @"<[^>]+>", "");
        
        // Decode common HTML entities
        html = System.Net.WebUtility.HtmlDecode(html);
        
        // Clean up whitespace
        html = Regex.Replace(html, @"[ \t]+", " ");
        html = Regex.Replace(html, @"\n\s*\n+", "\n\n");
        
        return html.Trim();
    }
}

// Additional models for full message
public class GraphMessageFull
{
    public string Id { get; set; } = "";
    public string? Subject { get; set; }
    public GraphEmailAddress? From { get; set; }
    public GraphEmailAddress[]? ToRecipients { get; set; }
    public GraphMessageBody? Body { get; set; }
    public DateTime ReceivedDateTime { get; set; }
    public bool IsRead { get; set; }
}

public class GraphMessageBody
{
    public string? ContentType { get; set; }
    public string? Content { get; set; }
}

public record GetHotmailContentsResponse(bool Success, HotmailMessageFull? Message, string? Error = null);

public record HotmailMessageFull(
    string Id,
    string Subject,
    string From,
    string To,
    string Body,
    DateTime ReceivedDateTime,
    bool IsRead
);
