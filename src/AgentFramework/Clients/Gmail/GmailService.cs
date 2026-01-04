using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace Agent.Clients.Gmail;

public class GmailService : IGmailService
{
    private readonly HttpClient _httpClient;
    private readonly GoogleAuthService _authService;

    private const string BaseUrl = "https://gmail.googleapis.com/gmail/v1/users/me";

    public GmailService(HttpClient httpClient, GoogleAuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }

    public async Task<List<GmailMessage>> GetMailAsync(int maxResults = 20)
    {
        return await FetchMessagesAsync(query: null, maxResults);
    }

    public async Task<List<GmailMessage>> SearchMailAsync(string query, int maxResults = 20)
    {
        return await FetchMessagesAsync(query, maxResults);
    }

    public async Task<GmailMessageDetail?> GetMailContentsAsync(string messageId)
    {
        var response = await GetMessageAsync(messageId);
        if (response is null)
            return null;

        return ConvertToMessageDetail(response);
    }

    private async Task<List<GmailMessage>> FetchMessagesAsync(string? query, int maxResults)
    {
        var messages = new List<GmailMessage>();

        var accessToken = await _authService.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(accessToken))
        {
            Console.WriteLine("[Gmail] Authentication required but not completed.");
            return messages;
        }

        // Step 1: Get message IDs from list endpoint
        var url = $"{BaseUrl}/messages?maxResults={maxResults}";
        if (!string.IsNullOrWhiteSpace(query))
            url += $"&q={Uri.EscapeDataString(query)}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        
        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[Gmail API Error] Status: {response.StatusCode}, Body: {errorBody}");
            return messages;
        }

        var listResponse = await response.Content.ReadFromJsonAsync<GmailListResponse>();
        if (listResponse?.Messages is null || listResponse.Messages.Length == 0)
            return messages;

        // Step 2: Fetch details for each message
        foreach (var messageRef in listResponse.Messages)
        {
            var messageResponse = await GetMessageAsync(messageRef.Id, accessToken);
            if (messageResponse is not null)
            {
                messages.Add(ConvertToMessage(messageResponse));
            }
        }

        return messages;
    }

    private async Task<GmailMessageResponse?> GetMessageAsync(string messageId, string? accessToken = null)
    {
        accessToken ??= await _authService.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(accessToken))
            return null;

        var url = $"{BaseUrl}/messages/{messageId}?format=full";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        
        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<GmailMessageResponse>();
    }

    private static GmailMessage ConvertToMessage(GmailMessageResponse response)
    {
        var headers = response.Payload?.Headers ?? [];
        
        return new GmailMessage(
            Id: response.Id,
            ThreadId: response.ThreadId,
            Snippet: response.Snippet,
            From: GetHeader(headers, "From"),
            To: GetHeader(headers, "To"),
            Subject: GetHeader(headers, "Subject"),
            Date: ParseDate(response.InternalDate)
        );
    }

    private static GmailMessageDetail ConvertToMessageDetail(GmailMessageResponse response)
    {
        var headers = response.Payload?.Headers ?? [];
        var body = ExtractBody(response.Payload);

        return new GmailMessageDetail(
            Id: response.Id,
            ThreadId: response.ThreadId,
            Snippet: response.Snippet,
            From: GetHeader(headers, "From"),
            To: GetHeader(headers, "To"),
            Subject: GetHeader(headers, "Subject"),
            Date: ParseDate(response.InternalDate),
            Body: body
        );
    }

    private static string GetHeader(MessageHeader[] headers, string name)
    {
        return headers.FirstOrDefault(h => 
            h.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
    }

    private static DateTime ParseDate(string internalDate)
    {
        if (long.TryParse(internalDate, out var epochMs))
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(epochMs).UtcDateTime;
        }
        return DateTime.MinValue;
    }

    private static string ExtractBody(MessagePayload? payload)
    {
        if (payload is null)
            return string.Empty;

        // Try to get body directly from payload
        if (payload.Body?.Data is not null)
        {
            return DecodeBase64Url(payload.Body.Data);
        }

        // Search through parts for text content
        if (payload.Parts is not null)
        {
            return ExtractBodyFromParts(payload.Parts);
        }

        return string.Empty;
    }

    private static string ExtractBodyFromParts(MessagePart[] parts)
    {
        // Prefer text/plain, fallback to text/html
        var textPart = parts.FirstOrDefault(p => p.MimeType == "text/plain");
        if (textPart?.Body?.Data is not null)
        {
            return DecodeBase64Url(textPart.Body.Data);
        }

        var htmlPart = parts.FirstOrDefault(p => p.MimeType == "text/html");
        if (htmlPart?.Body?.Data is not null)
        {
            return DecodeBase64Url(htmlPart.Body.Data);
        }

        // Recursively search nested parts
        foreach (var part in parts)
        {
            if (part.Parts is not null)
            {
                var body = ExtractBodyFromParts(part.Parts);
                if (!string.IsNullOrEmpty(body))
                    return body;
            }
        }

        return string.Empty;
    }

    private static string DecodeBase64Url(string base64Url)
    {
        try
        {
            // Convert base64url to standard base64
            var base64 = base64Url
                .Replace('-', '+')
                .Replace('_', '/');

            // Add padding if needed
            var padding = base64.Length % 4;
            if (padding > 0)
                base64 += new string('=', 4 - padding);

            var bytes = Convert.FromBase64String(base64);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return string.Empty;
        }
    }
}
