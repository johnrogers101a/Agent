using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using static GoogleApiClient.Constants;

namespace GoogleApiClient.Gmail;

/// <summary>
/// Handles Google OAuth2 authorization code flow with PKCE authentication.
/// </summary>
public class GoogleAuthService
{
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _tokenFilePath;

    private const string AuthorizationUrl = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenUrl = "https://oauth2.googleapis.com/token";
    private const string GmailReadonlyScope = "https://www.googleapis.com/auth/gmail.readonly";
    private const string RedirectUri = "http://localhost:8642/callback";
    private const int CallbackPort = 8642;

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

        // Need to do full authorization code flow
        var newToken = await PerformAuthorizationCodeFlowAsync();
        return newToken?.AccessToken;
    }

    /// <summary>
    /// Initiates the authorization code flow with PKCE.
    /// </summary>
    public async Task<GoogleTokenResponse?> PerformAuthorizationCodeFlowAsync()
    {
        // Generate PKCE code verifier and challenge
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);
        var state = GenerateState();

        // Build authorization URL
        var authUrl = BuildAuthorizationUrl(codeChallenge, state);

        Console.WriteLine();
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║           Gmail Authentication Required                     ║");
        Console.WriteLine("╠════════════════════════════════════════════════════════════╣");
        Console.WriteLine("║  A browser window will open for Google sign-in.            ║");
        Console.WriteLine("║  Please complete the authentication in your browser.       ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Start local HTTP listener for callback
        string? authorizationCode = null;
        string? returnedState = null;

        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{CallbackPort}/");
        
        try
        {
            listener.Start();
            Console.WriteLine($"[Gmail Auth] Listening for callback on port {CallbackPort}...");

            // Open browser
            OpenBrowser(authUrl);

            // Wait for callback with timeout
            var listenerTask = WaitForCallbackAsync(listener);
            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(5));

            var completedTask = await Task.WhenAny(listenerTask, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                Console.WriteLine("[Gmail Auth] Authentication timed out.");
                return null;
            }

            (authorizationCode, returnedState) = await listenerTask;
        }
        finally
        {
            listener.Stop();
        }

        // Validate state
        if (returnedState != state)
        {
            Console.WriteLine("[Gmail Auth] State mismatch - possible CSRF attack.");
            return null;
        }

        if (string.IsNullOrEmpty(authorizationCode))
        {
            Console.WriteLine("[Gmail Auth] No authorization code received.");
            return null;
        }

        // Exchange code for tokens
        var token = await ExchangeCodeForTokensAsync(authorizationCode, codeVerifier);
        if (token is not null)
        {
            await SaveTokenAsync(token);
            Console.WriteLine("[Gmail Auth] Authentication successful!");
        }

        return token;
    }

    private string BuildAuthorizationUrl(string codeChallenge, string state)
    {
        var parameters = new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["redirect_uri"] = RedirectUri,
            ["response_type"] = "code",
            ["scope"] = GmailReadonlyScope,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
            ["state"] = state,
            ["access_type"] = "offline",
            ["prompt"] = "consent"
        };

        var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        return $"{AuthorizationUrl}?{queryString}";
    }

    private async Task<(string? code, string? state)> WaitForCallbackAsync(HttpListener listener)
    {
        var context = await listener.GetContextAsync();
        var request = context.Request;
        var response = context.Response;

        string? code = request.QueryString["code"];
        string? state = request.QueryString["state"];
        string? error = request.QueryString["error"];

        string responseHtml;
        if (!string.IsNullOrEmpty(error))
        {
            responseHtml = $@"
<!DOCTYPE html>
<html>
<head><title>Authentication Failed</title></head>
<body style='font-family: Arial, sans-serif; text-align: center; padding-top: 50px;'>
    <h1 style='color: #d93025;'>❌ Authentication Failed</h1>
    <p>Error: {error}</p>
    <p>You can close this window.</p>
</body>
</html>";
            Console.WriteLine($"[Gmail Auth] Authentication error: {error}");
        }
        else if (!string.IsNullOrEmpty(code))
        {
            responseHtml = @"
<!DOCTYPE html>
<html>
<head><title>Authentication Successful</title></head>
<body style='font-family: Arial, sans-serif; text-align: center; padding-top: 50px;'>
    <h1 style='color: #34a853;'>✓ Authentication Successful</h1>
    <p>You can close this window and return to the application.</p>
</body>
</html>";
        }
        else
        {
            responseHtml = @"
<!DOCTYPE html>
<html>
<head><title>Authentication Error</title></head>
<body style='font-family: Arial, sans-serif; text-align: center; padding-top: 50px;'>
    <h1 style='color: #d93025;'>❌ Unexpected Response</h1>
    <p>No authorization code received.</p>
</body>
</html>";
        }

        var buffer = Encoding.UTF8.GetBytes(responseHtml);
        response.ContentType = "text/html";
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
        response.Close();

        return (code, state);
    }

    private async Task<GoogleTokenResponse?> ExchangeCodeForTokensAsync(string code, string codeVerifier)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["code"] = code,
            ["code_verifier"] = codeVerifier,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = RedirectUri
        });

        var response = await _httpClient.PostAsync(TokenUrl, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[Gmail Auth] Token exchange failed: {responseBody}");
            return null;
        }

        var token = JsonSerializer.Deserialize<GoogleTokenResponse>(responseBody);
        if (token is not null)
        {
            token.ExpiresAt = DateTime.UtcNow.AddSeconds(token.ExpiresIn);
            _cachedToken = token;
        }

        return token;
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

    private static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(bytes);
    }

    private static string GenerateState()
    {
        var bytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static void OpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            Console.WriteLine($"[Gmail Auth] Please open this URL manually: {url}");
        }
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
