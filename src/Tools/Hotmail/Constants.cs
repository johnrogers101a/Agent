namespace Hotmail;

internal static class Constants
{
    public const string GraphApiBase = "https://graph.microsoft.com/v1.0";
}

internal static class Errors
{
    public const string UnableToObtainToken = "Unable to obtain access token. Please re-authenticate.";
    public const string FailedToFetchMessages = "Failed to fetch messages. Status code: {0}";
}
