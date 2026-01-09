#nullable enable

using AgentFramework.Configuration;
using AgentFramework.Core;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AzureFunctions;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace AgentFramework.Extensions;

/// <summary>
/// Extension methods for configuring durable agents in Azure Functions.
/// </summary>
public static class DurableAgentBuilderExtensions
{
    /// <summary>
    /// Configures durable agents from appsettings.json.
    /// Auto-detects local vs Azure environment.
    /// </summary>
    public static FunctionsApplicationBuilder ConfigureDurableAgent(this FunctionsApplicationBuilder builder)
    {
        return builder.ConfigureDurableAgent("appsettings.json");
    }

    /// <summary>
    /// Configures durable agents from the specified configuration file.
    /// </summary>
    public static FunctionsApplicationBuilder ConfigureDurableAgent(
        this FunctionsApplicationBuilder builder,
        string configFileName)
    {
        // Add configuration
        builder.Configuration.AddJsonFile(configFileName, optional: false, reloadOnChange: true);

        // Try to load user secrets from the entry assembly
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly != null)
        {
            builder.Configuration.AddUserSecrets(entryAssembly, optional: true);
        }

        // Build temporary service provider for logging during startup
        var tempServices = new ServiceCollection();
        tempServices.AddLogging(config => config.AddConsole());
        using var tempProvider = tempServices.BuildServiceProvider();
        var loggerFactory = tempProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("AgentFramework.Durable.Startup");

        // Load settings
        var appSettings = builder.Configuration.Get<AppSettings>()!;

        // Detect environment
        var isLocal = IsLocalEnvironment();
        logger.LogInformation("Running in {Environment} mode", isLocal ? "local" : "Azure");

        // Load tool assemblies
        LoadToolAssemblies(logger);

        // Discover tools
        logger.LogInformation("Discovering MCP tools from loaded assemblies...");
        var discoveredTools = McpToolDiscovery.DiscoverAllTools();
        logger.LogInformation("Discovered {Count} tools: {Tools}", 
            discoveredTools.Count, string.Join(", ", discoveredTools.Keys));

        // Register settings and HttpClient
        builder.Services.AddSingleton(appSettings);
        builder.Services.AddHttpClient();

        // Register tool dependencies
        RegisterToolDependencies(builder.Services, appSettings, discoveredTools);

        // Build service provider for agent creation
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Configure durable agents
        builder.ConfigureDurableAgents(options =>
        {
            foreach (var agentSettings in appSettings.Agents)
            {
                var agent = CreateAgent(agentSettings, appSettings, serviceProvider);
                options.AddAIAgent(agent);
                logger.LogInformation("Registered durable agent: {Name}", agentSettings.Name);
            }
        });

        // Local development: add Swagger endpoint
        if (isLocal)
        {
            builder.Services.AddOpenApi();
            
            // Register callback to open browser after startup
            builder.Services.AddSingleton<IHostedService>(sp => 
                new BrowserLauncherService(appSettings.Provider.DevUIPort));
        }

        return builder;
    }

    /// <summary>
    /// Detects if running locally vs in Azure.
    /// </summary>
    private static bool IsLocalEnvironment()
    {
        // Multiple checks for Azure environment - any of these indicate Azure hosting
        var azureIndicators = new[]
        {
            "WEBSITE_INSTANCE_ID",     // Standard Azure App Service/Functions
            "WEBSITE_SITE_NAME",       // Azure App Service site name
            "FUNCTIONS_WORKER_RUNTIME", // Azure Functions runtime (but may be auto-set)
            "AZURE_FUNCTIONS_ENVIRONMENT", // Azure Functions environment
            "CONTAINER_NAME"           // Flex Consumption container
        };

        foreach (var indicator in azureIndicators)
        {
            var value = Environment.GetEnvironmentVariable(indicator);
            if (!string.IsNullOrEmpty(value))
            {
                return false; // Running in Azure
            }
        }

        return true; // Running locally
    }

    /// <summary>
    /// Creates an AIAgent from settings.
    /// </summary>
    private static AIAgent CreateAgent(
        AppSettings.AgentSettings agentSettings,
        AppSettings appSettings,
        IServiceProvider serviceProvider)
    {
        var instructionsLoader = new InstructionsLoader();
        var instructions = instructionsLoader.Load(agentSettings.InstructionsFileName);

        var tools = McpToolDiscovery.CreateToolsForAgent(
            agentSettings.Tools,
            agentSettings.ToolDescriptions,
            basePath: Path.GetDirectoryName(agentSettings.InstructionsFileName),
            serviceProvider: serviceProvider);

        var chatClient = CreateAzureOpenAIChatClient(appSettings.Provider);

        return chatClient.CreateAIAgent(
            instructions: instructions,
            name: agentSettings.Name,
            tools: tools.Cast<AITool>().ToList());
    }

    /// <summary>
    /// Creates an Azure OpenAI chat client using Azure AD authentication.
    /// </summary>
    private static IChatClient CreateAzureOpenAIChatClient(AppSettings.ProviderSettings provider)
    {
        var credential = new DefaultAzureCredential();
        var client = new Azure.AI.OpenAI.AzureOpenAIClient(
            new Uri(provider.Endpoint),
            credential);

        return client.GetChatClient(provider.ModelName).AsIChatClient();
    }

    /// <summary>
    /// Loads all assemblies from the application directory that might contain tools.
    /// </summary>
    private static void LoadToolAssemblies(ILogger logger)
    {
        var basePath = AppContext.BaseDirectory;
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .Select(a => a.GetName().Name)
            .ToHashSet();

        foreach (var dll in Directory.GetFiles(basePath, "*.dll"))
        {
            var assemblyName = Path.GetFileNameWithoutExtension(dll);

            if (loadedAssemblies.Contains(assemblyName) ||
                assemblyName.StartsWith("System") ||
                assemblyName.StartsWith("Microsoft") ||
                assemblyName.StartsWith("netstandard") ||
                assemblyName.StartsWith("mscorlib"))
                continue;

            try
            {
                var assembly = Assembly.LoadFrom(dll);
                var anyType = assembly.GetTypes().FirstOrDefault();
                if (anyType != null)
                {
                    RuntimeHelpers.RunClassConstructor(anyType.TypeHandle);
                }
                logger.LogDebug("Loaded assembly: {Assembly}", assemblyName);
            }
            catch
            {
                // Ignore assemblies that can't be loaded
            }
        }
    }

    /// <summary>
    /// Registers services that tools depend on based on constructor parameters.
    /// </summary>
    private static void RegisterToolDependencies(
        IServiceCollection services, 
        AppSettings appSettings, 
        Dictionary<string, DiscoveredTool> tools)
    {
        var registeredTypes = new HashSet<Type>();

        foreach (var tool in tools.Values)
        {
            var ctors = tool.DeclaringType.GetConstructors();
            foreach (var ctor in ctors)
            {
                foreach (var param in ctor.GetParameters())
                {
                    var paramType = param.ParameterType;

                    if (registeredTypes.Contains(paramType) ||
                        paramType == typeof(HttpClient) ||
                        paramType == typeof(IHttpClientFactory) ||
                        paramType == typeof(IConfiguration) ||
                        paramType.Namespace?.StartsWith("Microsoft.Extensions") == true)
                        continue;

                    if (paramType.FullName == "Gmail.AuthService")
                    {
                        services.AddSingleton(paramType, sp =>
                        {
                            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
                            var settings = sp.GetRequiredService<AppSettings>();
                            var clientId = settings.Clients.Gmail.ClientId;
                            var clientSecret = settings.Clients.Gmail.ClientSecret;
                            var ctorInfo = paramType.GetConstructors().First();
                            return ctorInfo.Invoke([http, clientId, clientSecret, "gmail_tokens.json"]);
                        });
                        registeredTypes.Add(paramType);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Hosted service that opens browser to Swagger on startup (local only).
    /// </summary>
    private class BrowserLauncherService : IHostedService
    {
        private readonly int _port;

        public BrowserLauncherService(int port)
        {
            _port = port > 0 ? port : 7071;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Extra safety: don't try to launch browser if we're somehow running in Azure
            // (shouldn't happen due to IsLocalEnvironment check, but defense in depth)
            try
            {
                // Only works on platforms with shell execute support (Windows, macOS)
                if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
                {
                    var swaggerUrl = $"http://localhost:{_port}/swagger";
                    Process.Start(new ProcessStartInfo(swaggerUrl) { UseShellExecute = true });
                }
            }
            catch
            {
                // Silently ignore - browser launch is convenience feature only
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
