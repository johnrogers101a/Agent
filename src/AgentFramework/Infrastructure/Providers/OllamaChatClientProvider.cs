using Microsoft.Extensions.AI;
using OllamaSharp;
using static Agent.Configuration.AppSettings;

namespace Agent.Infrastructure.Providers;

public class OllamaChatClientProvider : IChatClientProvider
{
    public IChatClient CreateClient(ProviderSettings settings)
    {
        return new OllamaApiClient(new Uri(settings.Endpoint), settings.ModelName);
    }
}
