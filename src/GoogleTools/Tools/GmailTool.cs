using System.ComponentModel;
using GoogleApiClient.Gmail;
using AgentFramework.Configuration;
using Microsoft.Extensions.AI;

namespace GoogleTools.Tools;

public static class GmailTool
{
    private static readonly AppSettings _settings = AppSettings.LoadConfiguration();
    private static readonly HttpClient _httpClient = new();
    private static readonly GoogleAuthService _authService = new(
        _httpClient, 
        _settings.Clients.Gmail.ClientId, 
        _settings.Clients.Gmail.ClientSecret);
    private static readonly IGmailService _gmailService = new GmailService(_httpClient, _authService);

    public static async Task<EmailSearchResult> GetMail()
    {
        var messages = await _gmailService.GetMailAsync(maxResults: 20);
        return new EmailSearchResult(messages.Count, null, messages);
    }

    public static async Task<EmailSearchResult> SearchMail(
        [Description("The Gmail search query")] string query)
    {
        var messages = await _gmailService.SearchMailAsync(query, maxResults: 20);
        return new EmailSearchResult(messages.Count, query, messages);
    }

    public static async Task<GmailMessageDetail?> GetMailContents(
        [Description("The Gmail message ID")] string messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId))
            return null;

        return await _gmailService.GetMailContentsAsync(messageId);
    }

    public static AIFunction CreateGetMail(string? description = null)
    {
        return AIFunctionFactory.Create(
            GetMail,
            name: "GetMail",
            description: description ?? "Gets the latest 20 emails from the user's Gmail inbox");
    }

    public static AIFunction CreateSearchMail(string? description = null)
    {
        return AIFunctionFactory.Create(
            SearchMail,
            name: "SearchMail",
            description: description ?? "Searches emails using Gmail search syntax");
    }

    public static AIFunction CreateGetMailContents(string? description = null)
    {
        return AIFunctionFactory.Create(
            GetMailContents,
            name: "GetMailContents",
            description: description ?? "Gets the full contents of a specific email by its ID");
    }
}
