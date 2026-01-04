using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using AgentFramework.Configuration;
using AgentFramework.Infrastructure;
using AgentFramework.Tools;

namespace AgentFramework.Core;

public static class AgentFactory
{
    private static readonly InstructionsLoader _instructionsLoader = new();
    private static ILoggerFactory? _loggerFactory;

    public static Dictionary<string, DevUIAwareAgent> Load(AppSettings appSettings, ToolRegistry toolRegistry)
    {
        // Set up logging
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var provider = ChatClientProviderFactory.GetProvider(appSettings.Provider.Type);
        var chatClient = provider.CreateClient(appSettings.Provider);

        var agents = new Dictionary<string, DevUIAwareAgent>();
        foreach (var a in appSettings.Agents)
        {
            var aiAgent = chatClient.CreateAIAgent(
                instructions: _instructionsLoader.Load(a.InstructionsFileName),
                name: a.Name,
                tools: toolRegistry.GetTools(a.Tools));

            var logger = _loggerFactory.CreateLogger(a.Name);
            agents[a.Name] = new DevUIAwareAgent(aiAgent, appSettings.Provider, logger);
        }
        return agents;
    }
}
