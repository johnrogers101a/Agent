using System.ComponentModel;
using GoogleApiClient.Gmail;
using GoogleApiClient.Models;
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

    public static async Task<EmailListResult> GetMail()
    {
        var messages = await _gmailService.GetMailAsync(maxResults: 20);

        return new EmailListResult(
            Count: messages.Count,
            Query: null,
            Emails: messages.Select(msg => new EmailSummary(
                Id: msg.Id,
                From: msg.From,
                Subject: msg.Subject,
                Date: msg.Date,
                Preview: msg.Snippet)).ToList());
    }

    public static async Task<EmailListResult> SearchMail(
        [Description("The Gmail search query")] string query)
    {
        var messages = await _gmailService.SearchMailAsync(query, maxResults: 20);

        return new EmailListResult(
            Count: messages.Count,
            Query: query,
            Emails: messages.Select(msg => new EmailSummary(
                Id: msg.Id,
                From: msg.From,
                Subject: msg.Subject,
                Date: msg.Date,
                Preview: msg.Snippet)).ToList());
    }

    public static async Task<EmailDetailResult?> GetMailContents(
        [Description("The Gmail message ID")] string messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId))
            return null;

        var message = await _gmailService.GetMailContentsAsync(messageId);
        
        if (message is null)
            return null;

        return new EmailDetailResult(
            Id: message.Id,
            From: message.From,
            To: message.To,
            Subject: message.Subject,
            Date: message.Date,
            Body: message.Body);
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
