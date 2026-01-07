using AgentFramework.Api;
using AgentFramework.Configuration;
using AgentFramework.Infrastructure;
using Microsoft.Extensions.AI;

namespace AgentFramework.Core;

public static class AgentFactory
{
    private static readonly InstructionsLoader _instructionsLoader = new();
    private static ILoggerFactory? _loggerFactory;

    public static Dictionary<string, DevUIAwareAgent> Load(AppSettings appSettings, IServiceProvider serviceProvider)
    {
        // Set up logging
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Discover all available tools from loaded assemblies
        var allTools = McpToolDiscovery.DiscoverAllTools();
        var logger = _loggerFactory.CreateLogger("AgentFramework.AgentFactory");
        logger.LogInformation("Discovered {Count} tools: {Tools}", allTools.Count, string.Join(", ", allTools.Keys));

        var provider = ChatClientProviderFactory.GetProvider(appSettings.Provider.Type);
        var baseChatClient = provider.CreateClient(appSettings.Provider);

        // Get context length from Ollama if using Ollama provider
        var contextLength = GetContextLengthAsync(appSettings.Provider).GetAwaiter().GetResult();

        var agents = new Dictionary<string, DevUIAwareAgent>();
        foreach (var a in appSettings.Agents)
        {
            // Create conversation memory for this agent
            var memory = new ConversationMemory(baseChatClient, contextLength);
            
            // Wrap the chat client with memory support
            var memoryChatClient = new MemoryDelegatingChatClient(baseChatClient, memory);

            // Get tools for this agent using McpToolDiscovery with DI support
            var tools = McpToolDiscovery.CreateToolsForAgent(
                a.Tools,
                a.ToolDescriptions,
                basePath: Path.GetDirectoryName(Path.GetFullPath("appsettings.json")),
                serviceProvider: serviceProvider);

            var aiAgent = memoryChatClient.CreateAIAgent(
                instructions: _instructionsLoader.Load(a.InstructionsFileName),
                name: a.Name,
                tools: tools.Cast<AITool>().ToList());

            var agentLogger = _loggerFactory.CreateLogger(a.Name);
            agents[a.Name] = new DevUIAwareAgent(aiAgent, memory, appSettings.Provider, agentLogger);
        }
        return agents;
    }

    /// <summary>
    /// Gets the context length from the provider, using Ollama API if applicable.
    /// </summary>
    private static async Task<int?> GetContextLengthAsync(AppSettings.ProviderSettings settings)
    {
        if (settings.Type != Constants.ProviderTypes.Ollama)
        {
            // For non-Ollama providers, return null to use default
            return null;
        }

        try
        {
            using var httpClient = new HttpClient
            {
                BaseAddress = new Uri(settings.Endpoint)
            };
            
            return await OllamaModelInfo.GetContextLengthAsync(httpClient, settings.ModelName);
        }
        catch
        {
            // On any error, return null to use default
            return null;
        }
    }
}
