#nullable enable

using System.Text.Json;
using System.Text.RegularExpressions;
using AgentFramework.Attributes;
using Hotmail.Models;
using Microsoft.Extensions.Logging;

namespace Hotmail.Tools;

/// <summary>
/// Retrieves recent emails from Hotmail/Outlook.com inbox.
/// </summary>
public class GetHotmail
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly TimeZoneInfo s_pacificZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
    private static readonly string[] s_scopes = ["https://graph.microsoft.com/Mail.Read"];

    private readonly HotmailClientFactory _clientFactory;
    private readonly HttpClient _httpClient;
    private readonly ILogger<GetHotmail> _logger;

    public GetHotmail(HotmailClientFactory clientFactory, HttpClient httpClient, ILogger<GetHotmail> logger)
    {
        _clientFactory = clientFactory;
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves recent emails from the user's Hotmail/Outlook.com inbox. Returns a list of email summaries including sender, subject, and preview.
    /// </summary>
    /// <param name="maxResults">Maximum number of emails to retrieve (default 20).</param>
    [McpTool]
    public async Task<GetHotmailResponse> ExecuteAsync(int maxResults = 20)
    {
        _logger.LogTrace("GetHotmail starting with MaxResults={MaxResults}", maxResults);

        string token;
        try
        {
            token = await _clientFactory.GetTokenAsync(s_scopes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetHotmail failed: Unable to obtain access token");
            return new GetHotmailResponse(false, [], Errors.UnableToObtainToken);
        }
        _logger.LogTrace("GetHotmail obtained access token");

        var url = $"{Constants.GraphApiBase}/me/mailfolders/inbox/messages?$top={maxResults}&$select=id,subject,from,toRecipients,bodyPreview,receivedDateTime,isRead&$orderby=receivedDateTime desc";
        
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        _logger.LogTrace("GetHotmail fetching messages from Graph API");
        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("GetHotmail failed to fetch messages: {StatusCode} {Error}", response.StatusCode, errorBody);
            return new GetHotmailResponse(false, [], string.Format(Errors.FailedToFetchMessages, response.StatusCode));
        }

        var json = await response.Content.ReadAsStringAsync();
        var listResponse = JsonSerializer.Deserialize<GraphMessageListResponse>(json, s_jsonOptions);
        
        if (listResponse?.Value is null || listResponse.Value.Length == 0)
        {
            _logger.LogTrace("GetHotmail found no messages");
            return new GetHotmailResponse(true, []);
        }

        _logger.LogTrace("GetHotmail found {Count} messages", listResponse.Value.Length);

        var messages = listResponse.Value.Select(msg => new HotmailMessage(
            msg.Id,
            msg.Subject ?? "(No Subject)",
            FormatEmailAddress(msg.From),
            FormatToRecipients(msg.ToRecipients),
            msg.BodyPreview ?? "",
            ConvertToPacific(msg.ReceivedDateTime),
            msg.IsRead
        )).ToList();

        _logger.LogTrace("GetHotmail completed successfully with {Count} messages", messages.Count);
        return new GetHotmailResponse(true, messages);
    }

    private static string FormatEmailAddress(GraphEmailAddress? addr)
    {
        if (addr?.EmailAddress == null)
            return "(Unknown)";
        
        var name = addr.EmailAddress.Name;
        var email = addr.EmailAddress.Address;
        
        if (string.IsNullOrEmpty(name) || name == email)
            return email ?? "(Unknown)";
        
        // Use parentheses instead of angle brackets for markdown safety
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
        // Graph API returns UTC times
        var utc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, s_pacificZone);
    }
}
