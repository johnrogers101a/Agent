using AgentFramework.Configuration;
using AgentFramework.Core;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
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
        
        // Ensure user secrets are loaded into the WebApplicationBuilder's configuration
        // This is necessary because tools injected via DI use IConfiguration, not AppSettings
        var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
        if (entryAssembly != null)
        {
            builder.Configuration.AddUserSecrets(entryAssembly, optional: true);
        }
        
        var appSettings = AppSettings.LoadConfiguration(configFileName);
        
        // Load all assemblies from the app directory to ensure tool assemblies are available
        LoadToolAssemblies(logger);
        
        // Discover tools from loaded assemblies
        logger.LogInformation("Discovering MCP tools from loaded assemblies...");
        var discoveredTools = McpToolDiscovery.DiscoverAllTools();
        logger.LogInformation("Discovered {Count} tools: {Tools}", discoveredTools.Count, string.Join(", ", discoveredTools.Keys));
        
        // Register settings and HttpClient
        builder.Services.AddSingleton(appSettings);
        builder.Services.AddHttpClient();
        
        // Add CORS for Chat app (allows any origin in development)
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });
        
        // Auto-register tool dependencies based on discovered tools
        RegisterToolDependencies(builder.Services, appSettings, discoveredTools);
        
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
            
            // Register AIAgent as a factory - DevUI will resolve this
            builder.Services.AddSingleton<AIAgent>(sp =>
            {
                var settings = sp.GetRequiredService<AppSettings>();
                var agents = AgentFactory.Load(settings, sp);
                return agents.Values.First().Agent;
            });
            
            builder.AddDevUI();
            builder.AddOpenAIResponses();
            builder.AddOpenAIConversations();
        }
        else
        {
            // API mode - add OpenAPI/Swagger support
            builder.Services.AddOpenApi();
        }
        
        return builder;
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
            
            // Skip already loaded and system assemblies
            if (loadedAssemblies.Contains(assemblyName) ||
                assemblyName.StartsWith("System") ||
                assemblyName.StartsWith("Microsoft") ||
                assemblyName.StartsWith("netstandard") ||
                assemblyName.StartsWith("mscorlib"))
                continue;
            
            try
            {
                var assembly = Assembly.LoadFrom(dll);
                // Force the assembly to initialize by running the class constructor of any type
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
    private static void RegisterToolDependencies(IServiceCollection services, AppSettings appSettings, Dictionary<string, DiscoveredTool> tools)
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
                    
                    // Skip types that are already registered or built-in
                    if (registeredTypes.Contains(paramType) ||
                        paramType == typeof(HttpClient) ||
                        paramType == typeof(IHttpClientFactory) ||
                        paramType == typeof(IConfiguration) ||
                        paramType.Namespace?.StartsWith("Microsoft.Extensions") == true)
                        continue;
                    
                    // Register Gmail.AuthService if a tool depends on it
                    if (paramType.FullName == "Gmail.AuthService")
                    {
                        // Register HttpClient for AuthService
                        services.AddSingleton(paramType, sp =>
                        {
                            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
                            var settings = sp.GetRequiredService<AppSettings>();
                            var clientId = settings.Clients.Gmail.ClientId;
                            var clientSecret = settings.Clients.Gmail.ClientSecret;
                            
                            // Use reflection to invoke the constructor with named parameters
                            var ctorInfo = paramType.GetConstructors().First();
                            return ctorInfo.Invoke([http, clientId, clientSecret, "gmail_tokens.json"]);
                        });
                        registeredTypes.Add(paramType);
                    }
                    
                    // Register Graph.GraphClientFactory if a tool depends on it
                    if (paramType.FullName == "Graph.GraphClientFactory")
                    {
                        services.AddSingleton(paramType, sp =>
                        {
                            var settings = sp.GetRequiredService<AppSettings>();
                            var graphSettings = settings.Clients.Graph;
                            
                            // Use reflection to invoke the constructor
                            var ctorInfo = paramType.GetConstructors().First();
                            return ctorInfo.Invoke([graphSettings]);
                        });
                        registeredTypes.Add(paramType);
                    }
                    
                    // Register Hotmail.HotmailClientFactory if a tool depends on it
                    if (paramType.FullName == "Hotmail.HotmailClientFactory")
                    {
                        services.AddSingleton(paramType, sp =>
                        {
                            var settings = sp.GetRequiredService<AppSettings>();
                            var hotmailSettings = settings.Clients.Hotmail;
                            
                            // Use reflection to invoke the constructor
                            var ctorInfo = paramType.GetConstructors().First();
                            return ctorInfo.Invoke([hotmailSettings]);
                        });
                        registeredTypes.Add(paramType);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Configures the agent runtime based on settings (DevUI or API mode).
    /// </summary>
    public static WebApplication UseAgents(this WebApplication app)
    {
        var appSettings = app.Services.GetRequiredService<AppSettings>();
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("AgentFramework.Startup");
        
        // Enable CORS for Chat app
        app.UseCors();
        
        if (appSettings.Provider.DevUI)
        {
            // DevUI mode - agents are created via DI factory, just configure endpoints
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
            // API mode - create agents and map Ollama-compatible endpoints
            var agents = AgentFactory.Load(appSettings, app.Services);
            app.MapOllamaEndpoints(agents, appSettings);
            
            // Map OpenAPI/Swagger
            app.MapOpenApi();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/v1.json", "Agent API");
            });
            
            var port = appSettings.Provider.DevUIPort > 0 ? appSettings.Provider.DevUIPort : 11435;
            var url = $"http://localhost:{port}";
            app.Urls.Add(url);
            
            logger.LogInformation(StartupMessages.OllamaApiRunning, port);
            logger.LogInformation(StartupMessages.AvailableEndpoints);
            logger.LogInformation(StartupMessages.EndpointGenerate);
            logger.LogInformation(StartupMessages.EndpointChat);
            logger.LogInformation(StartupMessages.EndpointTags);
            logger.LogInformation(StartupMessages.EndpointPs);
            
            // Open browser to Swagger UI on startup
            var swaggerUrl = $"{url}/swagger";
            app.Lifetime.ApplicationStarted.Register(() =>
            {
                Process.Start(new ProcessStartInfo(swaggerUrl) { UseShellExecute = true });
            });
        }
        
        return app;
    }
}
