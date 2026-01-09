// Copyright (c) 4JS. All rights reserved.

using AgentFramework.Configuration;
using AgentFramework.Core;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AzureFunctions;
using Microsoft.Agents.AI.OpenAI;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;

namespace AgentFramework;

/// <summary>
/// Hosts agents as Azure Functions with durable entities.
/// </summary>
public static class FunctionsHost
{
    /// <summary>
    /// Runs the agent as an Azure Function with durable task support.
    /// </summary>
    public static void Run(string[] args, AppSettings settings)
    {
        var chatClient = CreateChatClient(settings.Provider);
        var agents = CreateAgents(settings, chatClient);

        var builder = FunctionsApplication.CreateBuilder(args);
        
        // Configure durable agents - each agent gets its own HTTP endpoint
        builder.ConfigureDurableAgents(options =>
        {
            foreach (var agent in agents)
            {
                options.AddAIAgent(agent, timeToLive: TimeSpan.FromHours(1));
            }
        });

        using var app = builder.Build();
        app.Run();
    }

    private static IChatClient CreateChatClient(AppSettings.ProviderSettings provider)
    {
        var credential = new DefaultAzureCredential();
        var client = new AzureOpenAIClient(new Uri(provider.Endpoint), credential);
        return client.GetChatClient(provider.ModelName).AsIChatClient();
    }

    private static List<AIAgent> CreateAgents(AppSettings settings, IChatClient chatClient)
    {
        var instructionsLoader = new InstructionsLoader();
        var agents = new List<AIAgent>();

        foreach (var agentSettings in settings.Agents)
        {
            var instructions = instructionsLoader.Load(agentSettings.InstructionsFileName);
            
            // Create tools for this agent
            var tools = McpToolDiscovery.CreateToolsForAgent(
                agentSettings.Tools,
                agentSettings.ToolDescriptions,
                basePath: Path.GetDirectoryName(agentSettings.InstructionsFileName),
                serviceProvider: null); // Tools with DI dependencies won't work in FunctionMode yet

            var agent = chatClient.CreateAIAgent(
                instructions: instructions,
                name: agentSettings.Name,
                tools: tools.Cast<AITool>().ToList());

            agents.Add(agent);
        }

        return agents;
    }
}
