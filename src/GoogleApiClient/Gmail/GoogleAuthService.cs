using System.Net.Http.Json;
using System.Text.Json;
using static GoogleApiClient.Constants;

namespace GoogleApiClient.Gmail;

/// <summary>
/// Handles Google OAuth2 device code flow authentication.
/// </summary>
public class GoogleAuthService
{
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _tokenFilePath;

    private const string DeviceCodeUrl = "https://oauth2.googleapis.com/device/code";
    private const string TokenUrl = "https://oauth2.googleapis.com/token";
    private const string GmailReadonlyScope = "https://www.googleapis.com/auth/gmail.readonly";

    private GoogleTokenResponse? _cachedToken;

    public GoogleAuthService(HttpClient httpClient, string clientId, string clientSecret, string tokenFilePath = "gmail_tokens.json")
    {
        _httpClient = httpClient;
        _clientId = clientId;
        _clientSecret = clientSecret;
        _tokenFilePath = tokenFilePath;
    }

    /// <summary>
    /// Gets a valid access token, refreshing or re-authenticating as needed.
    /// </summary>
    public async Task<string?> GetAccessTokenAsync()
    {
        // Try to load cached token
        var token = await LoadTokenAsync();

        if (token is not null)
        {
            // Check if token is still valid (with 5 min buffer)
            if (token.ExpiresAt > DateTime.UtcNow.AddMinutes(5))
            {
                return token.AccessToken;
            }

            // Try to refresh
            if (!string.IsNullOrEmpty(token.RefreshToken))
            {
                var refreshed = await RefreshTokenAsync(token.RefreshToken);
                if (refreshed is not null)
                {
                    return refreshed.AccessToken;
                }
            }
        }

        // Need to do full device code flow
        var newToken = await PerformDeviceCodeFlowAsync();
        return newToken?.AccessToken;
    }

    /// <summary>
    /// Initiates the device code flow and waits for user to complete auth.
    /// </summary>
    public async Task<GoogleTokenResponse?> PerformDeviceCodeFlowAsync()
    {
        // Step 1: Request device code
        var deviceCode = await RequestDeviceCodeAsync();
        if (deviceCode is null)
        {
            Console.WriteLine(GmailAuthMessages.FailedToGetDeviceCode);
            return null;
        }

        // Step 2: Display instructions to user
        Console.WriteLine();
        Console.WriteLine(GmailAuthMessages.BoxTop);
        Console.WriteLine(GmailAuthMessages.BoxTitle);
        Console.WriteLine(GmailAuthMessages.BoxSeparator);
        Console.WriteLine(string.Format(GmailAuthMessages.BoxStep1, deviceCode.VerificationUrl));
        Console.WriteLine(string.Format(GmailAuthMessages.BoxStep2, deviceCode.UserCode));
        Console.WriteLine(GmailAuthMessages.BoxStep3);
        Console.WriteLine(GmailAuthMessages.BoxBottom);
        Console.WriteLine();

        // Step 3: Poll for token
        var token = await PollForTokenAsync(deviceCode);
        if (token is not null)
        {
            await SaveTokenAsync(token);
            Console.WriteLine(GmailAuthMessages.AuthenticationSuccessful);
        }

        return token;
    }

    private async Task<DeviceCodeResponse?> RequestDeviceCodeAsync()
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["scope"] = GmailReadonlyScope
        });

        var response = await _httpClient.PostAsync(DeviceCodeUrl, content);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine(string.Format(GmailAuthMessages.DeviceCodeRequestFailed, error));
            return null;
        }

        return await response.Content.ReadFromJsonAsync<DeviceCodeResponse>();
    }

    private async Task<GoogleTokenResponse?> PollForTokenAsync(DeviceCodeResponse deviceCode)
    {
        var pollInterval = TimeSpan.FromSeconds(deviceCode.Interval > 0 ? deviceCode.Interval : 5);
        var expiresAt = DateTime.UtcNow.AddSeconds(deviceCode.ExpiresIn);

        while (DateTime.UtcNow < expiresAt)
        {
            await Task.Delay(pollInterval);

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
                ["device_code"] = deviceCode.DeviceCode,
                ["grant_type"] = GoogleGrantTypes.DeviceCode
            });

            var response = await _httpClient.PostAsync(TokenUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var token = JsonSerializer.Deserialize<GoogleTokenResponse>(responseBody);
                if (token is not null)
                {
                    token.ExpiresAt = DateTime.UtcNow.AddSeconds(token.ExpiresIn);
                    _cachedToken = token;
                    return token;
                }
            }

            // Check for pending/slow_down errors
            var error = JsonSerializer.Deserialize<GoogleTokenError>(responseBody);
            if (error?.Error == GoogleOAuthErrors.AuthorizationPending)
            {
                // User hasn't completed auth yet, keep polling
                continue;
            }
            else if (error?.Error == GoogleOAuthErrors.SlowDown)
            {
                // Increase polling interval
                pollInterval = pollInterval.Add(TimeSpan.FromSeconds(5));
                continue;
            }
            else if (error?.Error == GoogleOAuthErrors.AccessDenied)
            {
                Console.WriteLine(GmailAuthMessages.AccessDenied);
                return null;
            }
            else if (error?.Error == GoogleOAuthErrors.ExpiredToken)
            {
                Console.WriteLine(GmailAuthMessages.DeviceCodeExpired);
                return null;
            }
            else
            {
                Console.WriteLine(string.Format(GmailAuthMessages.TokenError, error?.Error, error?.ErrorDescription));
                return null;
            }
        }

        Console.WriteLine(GmailAuthMessages.AuthenticationTimedOut);
        return null;
    }

    private async Task<GoogleTokenResponse?> RefreshTokenAsync(string refreshToken)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["refresh_token"] = refreshToken,
            ["grant_type"] = GoogleGrantTypes.RefreshToken
        });

        var response = await _httpClient.PostAsync(TokenUrl, content);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine(GmailAuthMessages.TokenRefreshFailed);
            return null;
        }

        var token = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>();
        if (token is not null)
        {
            // Preserve refresh token if not returned in response
            token.RefreshToken ??= refreshToken;
            token.ExpiresAt = DateTime.UtcNow.AddSeconds(token.ExpiresIn);
            _cachedToken = token;
            await SaveTokenAsync(token);
        }

        return token;
    }

    private async Task SaveTokenAsync(GoogleTokenResponse token)
    {
        try
        {
            var json = JsonSerializer.Serialize(token, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_tokenFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine(string.Format(GmailAuthMessages.FailedToSaveToken, ex.Message));
        }
    }

    private async Task<GoogleTokenResponse?> LoadTokenAsync()
    {
        if (_cachedToken is not null)
            return _cachedToken;

        try
        {
            if (!File.Exists(_tokenFilePath))
                return null;

            var json = await File.ReadAllTextAsync(_tokenFilePath);
            _cachedToken = JsonSerializer.Deserialize<GoogleTokenResponse>(json);
            return _cachedToken;
        }
        catch
        {
            return null;
        }
    }
}
