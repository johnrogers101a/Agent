using Azure.Core;
using Azure.Identity;
using AgentFramework.Configuration;

namespace Graph;

/// <summary>
/// Factory for acquiring Microsoft Graph API tokens using delegated authentication.
/// Tokens are cached automatically by Azure.Identity with persistent storage.
/// </summary>
public class GraphClientFactory
{
    private readonly AppSettings.GraphSettings _settings;
    private TokenCredential? _credential;
    private static bool _authInstructionsPrinted = false;

    public GraphClientFactory(AppSettings.GraphSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Gets an access token for the specified Graph API scopes.
    /// First call will trigger interactive authentication; subsequent calls use cached tokens.
    /// </summary>
    public async Task<string> GetTokenAsync(string[] scopes)
    {
        if (_credential == null)
        {
            // Print authentication instructions once before first auth attempt
            if (!_authInstructionsPrinted)
            {
                Console.WriteLine("\n" + new string('=', 80));
                Console.WriteLine("MICROSOFT GRAPH AUTHENTICATION REQUIRED");
                Console.WriteLine(new string('=', 80));
                Console.WriteLine($"Authentication Mode: {_settings.LoginMode}");
                Console.WriteLine($"Tenant: {_settings.TenantId}");
                Console.WriteLine($"Scopes: {string.Join(", ", scopes)}");
                
                if (_settings.LoginMode.Equals("Interactive", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("\nA browser window will open for you to sign in with your Microsoft account.");
                    Console.WriteLine("After signing in, the token will be cached for future requests.");
                }
                else
                {
                    Console.WriteLine("\nYou will receive a device code. Follow the instructions to authenticate.");
                    Console.WriteLine("After signing in, the token will be cached for future requests.");
                }
                Console.WriteLine(new string('=', 80) + "\n");
                _authInstructionsPrinted = true;
            }

            _credential = _settings.LoginMode.Equals("Interactive", StringComparison.OrdinalIgnoreCase)
                ? new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
                {
                    TenantId = _settings.TenantId,
                    ClientId = _settings.ClientId,
                    TokenCachePersistenceOptions = GetCacheOptions()
                })
                : new DeviceCodeCredential(new DeviceCodeCredentialOptions
                {
                    TenantId = _settings.TenantId,
                    ClientId = _settings.ClientId,
                    DeviceCodeCallback = (code, cancellation) =>
                    {
                        Console.WriteLine("\n" + new string('=', 80));
                        Console.WriteLine("DEVICE CODE AUTHENTICATION");
                        Console.WriteLine(new string('=', 80));
                        Console.WriteLine(code.Message);
                        Console.WriteLine("\nThis token will be cached for future requests.");
                        Console.WriteLine(new string('=', 80) + "\n");
                        return Task.CompletedTask;
                    },
                    TokenCachePersistenceOptions = GetCacheOptions()
                });
        }

        var tokenContext = new TokenRequestContext(scopes);
        var token = await _credential.GetTokenAsync(tokenContext, default);
        
        Console.WriteLine($"[Graph Auth] Token acquired successfully (expires: {token.ExpiresOn:yyyy-MM-dd HH:mm:ss})");
        
        return token.Token;
    }

    private TokenCachePersistenceOptions? GetCacheOptions()
    {
        if (string.IsNullOrWhiteSpace(_settings.TokenCachePath))
            return null;

        var cacheDir = Path.GetDirectoryName(_settings.TokenCachePath);
        if (!string.IsNullOrEmpty(cacheDir) && !Directory.Exists(cacheDir))
            Directory.CreateDirectory(cacheDir);

        return new TokenCachePersistenceOptions
        {
            Name = Path.GetFileName(_settings.TokenCachePath)
        };
    }
}
