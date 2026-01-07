#nullable enable

using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gmail;

/// <summary>
/// Handles Google OAuth2 with PKCE. Simple token management.
/// </summary>
public class AuthService
{
    private readonly HttpClient _http;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _tokenFile;
    private TokenData? _token;

    public AuthService(HttpClient http, string clientId, string clientSecret, string tokenFile = Defaults.TokenFile)
    {
        _http = http;
        _clientId = clientId;
        _clientSecret = clientSecret;
        _tokenFile = tokenFile;
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        _token ??= await LoadTokenAsync();

        if (_token?.ExpiresAt > DateTime.UtcNow.AddMinutes(5))
            return _token.AccessToken;

        if (_token?.RefreshToken is not null)
        {
            var refreshed = await RefreshAsync(_token.RefreshToken);
            if (refreshed is not null) return refreshed.AccessToken;
        }

        var newToken = await AuthenticateAsync();
        return newToken?.AccessToken;
    }

    private async Task<TokenData?> AuthenticateAsync()
    {
        var verifier = GenerateRandom(32);
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var state = GenerateRandom(16);

        var authUrl = $"{Urls.GoogleAuth}?client_id={_clientId}&redirect_uri={Uri.EscapeDataString(Urls.OAuthCallback)}" +
                      $"&response_type=code&scope={Uri.EscapeDataString(Urls.GmailScope)}" +
                      $"&code_challenge={challenge}&code_challenge_method=S256&state={state}&access_type=offline&prompt=consent";

        using var listener = new HttpListener();
        listener.Prefixes.Add(Urls.OAuthListener);
        listener.Start();

        OpenBrowser(authUrl);

        var ctx = await listener.GetContextAsync();
        var code = ctx.Request.QueryString["code"];
        
        await RespondAsync(ctx.Response, code is not null
            ? "<h1 style='color:green'>✓ Success</h1><p>You can close this window.</p>"
            : "<h1 style='color:red'>✗ Failed</h1>");
        
        listener.Stop();

        if (code is null) return null;

        return await ExchangeCodeAsync(code, verifier);
    }

    private async Task<TokenData?> ExchangeCodeAsync(string code, string verifier)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["code"] = code,
            ["code_verifier"] = verifier,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = Urls.OAuthCallback
        });

        var response = await _http.PostAsync(Urls.GoogleToken, content);
        if (!response.IsSuccessStatusCode) return null;

        var token = await response.Content.ReadFromJsonAsync<TokenData>();
        if (token is not null)
        {
            token.ExpiresAt = DateTime.UtcNow.AddSeconds(token.ExpiresIn);
            await SaveTokenAsync(token);
            _token = token;
        }
        return token;
    }

    private async Task<TokenData?> RefreshAsync(string refreshToken)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token"
        });

        var response = await _http.PostAsync(Urls.GoogleToken, content);
        if (!response.IsSuccessStatusCode) return null;

        var token = await response.Content.ReadFromJsonAsync<TokenData>();
        if (token is not null)
        {
            token.RefreshToken ??= refreshToken;
            token.ExpiresAt = DateTime.UtcNow.AddSeconds(token.ExpiresIn);
            await SaveTokenAsync(token);
            _token = token;
        }
        return token;
    }

    private async Task<TokenData?> LoadTokenAsync()
    {
        if (!File.Exists(_tokenFile)) return null;
        try { return JsonSerializer.Deserialize<TokenData>(await File.ReadAllTextAsync(_tokenFile)); }
        catch { return null; }
    }

    private async Task SaveTokenAsync(TokenData token)
    {
        try { await File.WriteAllTextAsync(_tokenFile, JsonSerializer.Serialize(token)); }
        catch { /* ignore */ }
    }

    private static string GenerateRandom(int bytes)
    {
        var buffer = new byte[bytes];
        RandomNumberGenerator.Fill(buffer);
        return Base64Url(buffer);
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static void OpenBrowser(string url)
    {
        try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
        catch { /* ignore */ }
    }

    private static async Task RespondAsync(HttpListenerResponse response, string html)
    {
        var body = $"<html><body style='font-family:sans-serif;text-align:center;padding:50px'>{html}</body></html>";
        var buffer = Encoding.UTF8.GetBytes(body);
        response.ContentType = "text/html";
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
        response.Close();
    }
}
