using Microsoft.Extensions.AI;
using static AgentFramework.Configuration.AppSettings;

namespace AgentFramework.Infrastructure.Providers;

public interface IChatClientProvider
{
    IChatClient CreateClient(ProviderSettings settings);
}
