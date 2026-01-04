using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using static AgentFramework.Configuration.AppSettings;

namespace AgentFramework.Infrastructure.Providers;

public class AzureFoundryChatClientProvider : IChatClientProvider
{
    public IChatClient CreateClient(ProviderSettings settings)
    {
        var client = string.IsNullOrEmpty(settings.ApiKey)
            ? new AzureOpenAIClient(new Uri(settings.Endpoint), new AzureCliCredential())
            : new AzureOpenAIClient(new Uri(settings.Endpoint), new AzureKeyCredential(settings.ApiKey));

        return client
            .GetChatClient(settings.ModelName)
            .AsIChatClient();
    }
}
