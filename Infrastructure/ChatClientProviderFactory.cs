using Agent.Infrastructure.Providers;

namespace Agent.Infrastructure;

public static class ChatClientProviderFactory
{
    private static readonly Dictionary<string, Func<IChatClientProvider>> _providers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Ollama"] = () => new OllamaChatClientProvider(),
        ["AzureFoundry"] = () => new AzureFoundryChatClientProvider(),
    };

    public static IChatClientProvider GetProvider(string providerType)
    {
        if (!_providers.TryGetValue(providerType, out var factory))
            throw new NotSupportedException($"Provider '{providerType}' is not supported.");

        return factory();
    }
}
