using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.OpenAI;
using OpenTelemetry;
using OpenTelemetry.Trace;
using AgentFramework.Configuration;
using AgentFramework.Core;
using static AgentFramework.Constants;

namespace AgentFramework.Extensions;

public static class AgentBuilderExtensions
{
    /// <summary>
    /// Configures the agent framework by loading settings, tools, and agents.
    /// </summary>
    public static WebApplicationBuilder ConfigureAgent(this WebApplicationBuilder builder)
    {
        return builder.ConfigureAgent("appsettings.json");
    }

    /// <summary>
    /// Configures the agent framework by loading settings, tools, and agents from the specified config file.
    /// </summary>
    public static WebApplicationBuilder ConfigureAgent(this WebApplicationBuilder builder, string configFileName)
    {
        // Build a temporary service provider to get loggers for startup
        var tempServices = new ServiceCollection();
        tempServices.AddLogging(config => config.AddConsole());
        using var tempProvider = tempServices.BuildServiceProvider();
        var loggerFactory = tempProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("AgentFramework.Startup");
        
        var appSettings = AppSettings.LoadConfiguration(configFileName);
        
        // Discover tools from loaded assemblies (triggered by referencing tool assemblies)
        logger.LogInformation("Discovering MCP tools from loaded assemblies...");
        var discoveredTools = McpToolDiscovery.DiscoverAllTools();
        logger.LogInformation("Discovered {Count} tools: {Tools}", discoveredTools.Count, string.Join(", ", discoveredTools.Keys));
        
        // Register settings - agents will be created in UseAgents after DI is available
        builder.Services.AddSingleton(appSettings);
        builder.Services.AddHttpClient();
        
        // If DevUI mode, add DevUI services and enable instrumentation/tracing
        if (appSettings.Provider.DevUI)
        {
            // Enable DevUI tracing via environment variable
            Environment.SetEnvironmentVariable("ENABLE_INSTRUMENTATION", "true");
            
            // Set up OpenTelemetry tracing to console
            Sdk.CreateTracerProviderBuilder()
                .AddSource("*Microsoft.Agents.AI")
                .AddSource("Microsoft.Agents.AI")
                .AddConsoleExporter()
                .Build();
            
            builder.AddDevUI();
            builder.AddOpenAIResponses();
            builder.AddOpenAIConversations();
        }
        
        return builder;
    }

    /// <summary>
    /// Configures the agent runtime based on settings (DevUI or API mode).
    /// </summary>
    public static WebApplication UseAgents(this WebApplication app)
    {
        var appSettings = app.Services.GetRequiredService<AppSettings>();
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("AgentFramework.Startup");
        
        // Create agents now that DI is available
        var agents = AgentFactory.Load(appSettings, app.Services);
        
        if (appSettings.Provider.DevUI)
        {
            // Register the first agent for DevUI
            var agent = agents.Values.First();
            
            // DevUI mode - configure DevUI endpoints
            app.MapOpenAIResponses();
            app.MapOpenAIConversations();
            app.MapDevUI();
            
            var url = $"http://localhost:{appSettings.Provider.DevUIPort}";
            app.Urls.Add(url);
            
            var devUIUrl = $"{url}/devui";
            logger.LogInformation(StartupMessages.DevUIAvailable, devUIUrl);
            
            // Open browser on startup
            app.Lifetime.ApplicationStarted.Register(() =>
            {
                Process.Start(new ProcessStartInfo(devUIUrl) { UseShellExecute = true });
            });
        }
        else
        {
            // API mode - map Ollama-compatible endpoints
            app.MapOllamaEndpoints(agents, appSettings);
            
            var port = appSettings.Provider.DevUIPort > 0 ? appSettings.Provider.DevUIPort : 11435;
            app.Urls.Add($"http://localhost:{port}");
            
            logger.LogInformation(StartupMessages.OllamaApiRunning, port);
            logger.LogInformation(StartupMessages.AvailableEndpoints);
            logger.LogInformation(StartupMessages.EndpointGenerate);
            logger.LogInformation(StartupMessages.EndpointChat);
            logger.LogInformation(StartupMessages.EndpointTags);
            logger.LogInformation(StartupMessages.EndpointPs);
        }
        
        return app;
    }
}
