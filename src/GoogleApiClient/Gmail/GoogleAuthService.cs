using System.Net.Http.Json;
using System.Text.Json;

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
            Console.WriteLine("[Gmail Auth] Failed to get device code.");
            return null;
        }

        // Step 2: Display instructions to user
        Console.WriteLine();
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║              Gmail Authentication Required                  ║");
        Console.WriteLine("╠════════════════════════════════════════════════════════════╣");
        Console.WriteLine($"║  1. Go to: {deviceCode.VerificationUrl,-45} ║");
        Console.WriteLine($"║  2. Enter code: {deviceCode.UserCode,-40} ║");
        Console.WriteLine("║  3. Grant access to your Gmail                             ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Step 3: Poll for token
        var token = await PollForTokenAsync(deviceCode);
        if (token is not null)
        {
            await SaveTokenAsync(token);
            Console.WriteLine("[Gmail Auth] Authentication successful!");
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
            Console.WriteLine($"[Gmail Auth] Device code request failed: {error}");
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
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code"
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
            if (error?.Error == "authorization_pending")
            {
                // User hasn't completed auth yet, keep polling
                continue;
            }
            else if (error?.Error == "slow_down")
            {
                // Increase polling interval
                pollInterval = pollInterval.Add(TimeSpan.FromSeconds(5));
                continue;
            }
            else if (error?.Error == "access_denied")
            {
                Console.WriteLine("[Gmail Auth] Access denied by user.");
                return null;
            }
            else if (error?.Error == "expired_token")
            {
                Console.WriteLine("[Gmail Auth] Device code expired. Please try again.");
                return null;
            }
            else
            {
                Console.WriteLine($"[Gmail Auth] Token error: {error?.Error} - {error?.ErrorDescription}");
                return null;
            }
        }

        Console.WriteLine("[Gmail Auth] Authentication timed out.");
        return null;
    }

    private async Task<GoogleTokenResponse?> RefreshTokenAsync(string refreshToken)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token"
        });

        var response = await _httpClient.PostAsync(TokenUrl, content);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine("[Gmail Auth] Token refresh failed, re-authentication required.");
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
            Console.WriteLine($"[Gmail Auth] Failed to save token: {ex.Message}");
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
