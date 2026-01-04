using System.ComponentModel;
using System.Text;
using Agent.Clients.Gmail;
using Agent.Configuration;
using Microsoft.Extensions.AI;

namespace Agent.Tools;

public static class GmailTool
{
    private static readonly AppSettings _settings = AppSettings.LoadConfiguration();
    private static readonly HttpClient _httpClient = new();
    private static readonly GoogleAuthService _authService = new(
        _httpClient, 
        _settings.Clients.Gmail.ClientId, 
        _settings.Clients.Gmail.ClientSecret);
    private static readonly IGmailService _gmailService = new GmailService(_httpClient, _authService);

    [Description("Gets the latest 20 emails from the user's Gmail inbox")]
    public static async Task<string> GetMail()
    {
        var messages = await _gmailService.GetMailAsync(maxResults: 20);
        
        if (messages.Count == 0)
            return "No emails found in inbox. You may need to authenticate - check the console for instructions.";

        var sb = new StringBuilder();
        sb.AppendLine($"Found {messages.Count} emails:\n");

        foreach (var msg in messages)
        {
            sb.AppendLine($"ID: {msg.Id}");
            sb.AppendLine($"From: {msg.From}");
            sb.AppendLine($"Subject: {msg.Subject}");
            sb.AppendLine($"Date: {msg.Date:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"Preview: {msg.Snippet}");
            sb.AppendLine("---");
        }

        return sb.ToString();
    }

    [Description("Searches emails using Gmail search syntax (e.g., 'from:user@example.com', 'is:unread', 'subject:hello')")]
    public static async Task<string> SearchMail(
        [Description("The Gmail search query")] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return "Please provide a search query.";

        var messages = await _gmailService.SearchMailAsync(query, maxResults: 20);
        
        if (messages.Count == 0)
            return $"No emails found matching query: {query}";

        var sb = new StringBuilder();
        sb.AppendLine($"Found {messages.Count} emails matching '{query}':\n");

        foreach (var msg in messages)
        {
            sb.AppendLine($"ID: {msg.Id}");
            sb.AppendLine($"From: {msg.From}");
            sb.AppendLine($"Subject: {msg.Subject}");
            sb.AppendLine($"Date: {msg.Date:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"Preview: {msg.Snippet}");
            sb.AppendLine("---");
        }

        return sb.ToString();
    }

    [Description("Gets the full contents of a specific email by its ID")]
    public static async Task<string> GetMailContents(
        [Description("The Gmail message ID")] string messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId))
            return "Please provide a message ID.";

        var message = await _gmailService.GetMailContentsAsync(messageId);
        
        if (message is null)
            return $"Unable to find email with ID: {messageId}";

        var sb = new StringBuilder();
        sb.AppendLine($"From: {message.From}");
        sb.AppendLine($"To: {message.To}");
        sb.AppendLine($"Subject: {message.Subject}");
        sb.AppendLine($"Date: {message.Date:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        sb.AppendLine("Body:");
        sb.AppendLine(message.Body);

        return sb.ToString();
    }

    public static AIFunction CreateGetMail()
    {
        return AIFunctionFactory.Create(
            GetMail,
            name: "GetMail",
            description: "Gets the latest 20 emails from the user's Gmail inbox");
    }

    public static AIFunction CreateSearchMail()
    {
        return AIFunctionFactory.Create(
            SearchMail,
            name: "SearchMail",
            description: "Searches emails using Gmail search syntax (e.g., 'from:user@example.com', 'is:unread', 'subject:hello')");
    }

    public static AIFunction CreateGetMailContents()
    {
        return AIFunctionFactory.Create(
            GetMailContents,
            name: "GetMailContents",
            description: "Gets the full contents of a specific email by its ID");
    }
}
