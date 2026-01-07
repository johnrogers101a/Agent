#nullable enable

namespace Gmail;

internal static class Urls
{
    public const string GmailApi = "https://gmail.googleapis.com/gmail/v1/users/me";
    public const string GoogleAuth = "https://accounts.google.com/o/oauth2/v2/auth";
    public const string GoogleToken = "https://oauth2.googleapis.com/token";
    public const string GmailScope = "https://www.googleapis.com/auth/gmail.readonly";
    public const string OAuthCallback = "http://localhost:8642/callback";
    public const string OAuthListener = "http://localhost:8642/";
}

internal static class ConfigKeys
{
    public const string ClientId = "Clients:Gmail:ClientId";
    public const string ClientSecret = "Clients:Gmail:ClientSecret";
}

internal static class Defaults
{
    public const string TokenFile = "gmail_tokens.json";
}

internal static class Errors
{
    public const string QueryRequired = "Query is required";
    public const string MessageIdRequired = "MessageId is required";
    public const string UnableToObtainToken = "Unable to obtain access token";
    public const string FailedToFetchMessages = "Failed to fetch messages: {0}";
    public const string FailedToSearchMessages = "Failed to search messages: {0}";
    public const string FailedToFetchMessage = "Failed to fetch message: {0}";
    public const string MessageNotFound = "Message not found";
}
