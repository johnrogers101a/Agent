namespace GoogleApiClient;

/// <summary>
/// Constants used across the GoogleApiClient library.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Gmail authentication console messages.
    /// </summary>
    public static class GmailAuthMessages
    {
        public const string LogPrefix = "[Gmail Auth] ";
        public const string FailedToGetDeviceCode = LogPrefix + "Failed to get device code.";
        public const string AuthenticationSuccessful = LogPrefix + "Authentication successful!";
        public const string DeviceCodeRequestFailed = LogPrefix + "Device code request failed: {0}";
        public const string AccessDenied = LogPrefix + "Access denied by user.";
        public const string DeviceCodeExpired = LogPrefix + "Device code expired. Please try again.";
        public const string TokenError = LogPrefix + "Token error: {0} - {1}";
        public const string AuthenticationTimedOut = LogPrefix + "Authentication timed out.";
        public const string TokenRefreshFailed = LogPrefix + "Token refresh failed, re-authentication required.";
        public const string FailedToSaveToken = LogPrefix + "Failed to save token: {0}";

        // Auth dialog box art
        public const string BoxTop = "╔════════════════════════════════════════════════════════════╗";
        public const string BoxTitle = "║              Gmail Authentication Required                  ║";
        public const string BoxSeparator = "╠════════════════════════════════════════════════════════════╣";
        public const string BoxBottom = "╚════════════════════════════════════════════════════════════╝";
        public const string BoxStep1 = "║  1. Go to: {0,-45} ║";
        public const string BoxStep2 = "║  2. Enter code: {0,-40} ║";
        public const string BoxStep3 = "║  3. Grant access to your Gmail                             ║";
    }

    /// <summary>
    /// Gmail API console messages.
    /// </summary>
    public static class GmailApiMessages
    {
        public const string LogPrefix = "[Gmail] ";
        public const string ErrorLogPrefix = "[Gmail API Error] ";
        public const string AuthenticationRequired = LogPrefix + "Authentication required but not completed.";
        public const string ApiError = ErrorLogPrefix + "Status: {0}, Body: {1}";
    }

    /// <summary>
    /// OAuth2 error codes from Google.
    /// </summary>
    public static class GoogleOAuthErrors
    {
        public const string AuthorizationPending = "authorization_pending";
        public const string SlowDown = "slow_down";
        public const string AccessDenied = "access_denied";
        public const string ExpiredToken = "expired_token";
    }

    /// <summary>
    /// OAuth2 grant types.
    /// </summary>
    public static class GoogleGrantTypes
    {
        public const string DeviceCode = "urn:ietf:params:oauth:grant-type:device_code";
        public const string RefreshToken = "refresh_token";
    }
}
