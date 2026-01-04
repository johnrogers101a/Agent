using Microsoft.Extensions.AI;
using static Agent.Configuration.AppSettings;

namespace Agent.Infrastructure.Providers;

public interface IChatClientProvider
{
    IChatClient CreateClient(ProviderSettings settings);
}
