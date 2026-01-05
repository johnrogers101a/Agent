using AgentFramework.Infrastructure.Providers;
using static AgentFramework.Constants;

namespace AgentFramework.Infrastructure;

public static class ChatClientProviderFactory
{
    private static readonly Dictionary<string, Func<IChatClientProvider>> _providers = new(StringComparer.OrdinalIgnoreCase)
    {
        [ProviderTypes.Ollama] = () => new OllamaChatClientProvider(),
        [ProviderTypes.AzureFoundry] = () => new AzureFoundryChatClientProvider(),
    };

    public static IChatClientProvider GetProvider(string providerType)
    {
        if (!_providers.TryGetValue(providerType, out var factory))
            throw new NotSupportedException(string.Format(ErrorMessages.ProviderNotSupported, providerType));

        return factory();
    }
}
