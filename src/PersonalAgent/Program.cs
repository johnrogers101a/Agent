using AgentFramework.Configuration;
using AgentFramework.Core;
using PersonalAgent.Api;
using PersonalAgent.Tools;

var builder = WebApplication.CreateBuilder(args);

// Load configuration and create agents
var appSettings = AppSettings.LoadConfiguration();
var toolRegistry = new ToolRegistry();
var agents = AgentFactory.Load(appSettings, toolRegistry);

var app = builder.Build();

// Map Ollama-compatible endpoints
app.MapOllamaEndpoints(agents, appSettings);

// Optional: Run in console mode if DevUI is disabled and a prompt is provided
if (!appSettings.Provider.DevUI && args.Length > 0)
{
    var prompt = string.Join(" ", args);
    await agents.First().Value.RunAsync(prompt);
}
else if (appSettings.Provider.DevUI)
{
    // DevUI mode - run the first agent with DevUI
    await agents.First().Value.RunAsync();
}
else
{
    // API server mode
    var port = appSettings.Provider.DevUIPort > 0 ? appSettings.Provider.DevUIPort : 11435;
    app.Urls.Add($"http://localhost:{port}");
    
    Console.WriteLine($"PersonalAgent Ollama-compatible API running at http://localhost:{port}");
    Console.WriteLine("Available endpoints:");
    Console.WriteLine("  POST /api/generate - Generate completion");
    Console.WriteLine("  POST /api/chat     - Chat completion");
    Console.WriteLine("  GET  /api/tags     - List models");
    Console.WriteLine("  GET  /api/ps       - List running models");
    Console.WriteLine();
    Console.WriteLine("Press Ctrl+C to stop...");

    await app.RunAsync();
}
