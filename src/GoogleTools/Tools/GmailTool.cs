using System.ComponentModel;
using GoogleApiClient.Gmail;
using AgentFramework.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GoogleTools.Tools;

public static class GmailTool
{
    private static readonly AppSettings _settings = AppSettings.LoadConfiguration();
    private static readonly HttpClient _httpClient = new();
    private static ILogger<GoogleAuthService>? _authLogger;
    private static ILogger<GmailService>? _gmailLogger;
    private static GoogleAuthService? _authService;
    private static IGmailService? _gmailService;

    public static void Initialize(ILoggerFactory? loggerFactory)
    {
        loggerFactory ??= NullLoggerFactory.Instance;
        _authLogger = loggerFactory.CreateLogger<GoogleAuthService>();
        _gmailLogger = loggerFactory.CreateLogger<GmailService>();
        _authService = new GoogleAuthService(
            _httpClient,
            _settings.Clients.Gmail.ClientId,
            _settings.Clients.Gmail.ClientSecret,
            _authLogger);
        _gmailService = new GmailService(_httpClient, _authService, _gmailLogger);
    }

    private static IGmailService GetGmailService()
    {
        if (_gmailService is null)
        {
            Initialize(null);
        }
        return _gmailService!;
    }

    public static async Task<EmailSearchResult> GetMail()
    {
        var messages = await GetGmailService().GetMailAsync(maxResults: 20);
        return new EmailSearchResult(messages.Count, null, messages);
    }

    public static async Task<EmailSearchResult> SearchMail(
        [Description("Gmail search query")] string query)
    {
        var messages = await GetGmailService().SearchMailAsync(query, maxResults: 20);
        return new EmailSearchResult(messages.Count, query, messages);
    }

    public static async Task<GmailMessageDetail?> GetMailContents(
        [Description("Email message ID")] string messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId))
            return null;

        return await GetGmailService().GetMailContentsAsync(messageId);
    }

    public static AIFunction CreateGetMail(string description, ILoggerFactory? loggerFactory = null)
    {
        if (_gmailService is null)
            Initialize(loggerFactory);
            
        return AIFunctionFactory.Create(
            GetMail,
            name: "GetMail",
            description: description);
    }

    public static AIFunction CreateSearchMail(string description, ILoggerFactory? loggerFactory = null)
    {
        if (_gmailService is null)
            Initialize(loggerFactory);
            
        return AIFunctionFactory.Create(
            SearchMail,
            name: "SearchMail",
            description: description);
    }

    public static AIFunction CreateGetMailContents(string description, ILoggerFactory? loggerFactory = null)
    {
        if (_gmailService is null)
            Initialize(loggerFactory);
            
        return AIFunctionFactory.Create(
            GetMailContents,
            name: "GetMailContents",
            description: description);
    }
}
