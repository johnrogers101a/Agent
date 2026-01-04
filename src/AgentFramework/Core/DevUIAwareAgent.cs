using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.OpenAI;
using static Agent.Configuration.AppSettings;

namespace Agent.Core;

/// <summary>
/// Wraps an AIAgent to provide DevUI support or console logging based on settings.
/// </summary>
public class DevUIAwareAgent
{
    private readonly AIAgent _agent;
    private readonly ProviderSettings _providerSettings;
    private readonly ILogger _logger;

    public DevUIAwareAgent(
        AIAgent agent,
        ProviderSettings providerSettings,
        ILogger logger)
    {
        _agent = agent;
        _providerSettings = providerSettings;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await RunAsync(string.Empty, cancellationToken);
    }

    public async Task RunAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (_providerSettings.DevUI)
        {
            await RunWithDevUIAsync(cancellationToken);
        }
        else
        {
            await RunConsoleAsync(prompt, cancellationToken);
        }
    }

    private async Task RunConsoleAsync(string prompt, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Agent '{AgentName}' processing: {Prompt}", _agent.Name, prompt);

        var response = await _agent.RunAsync(prompt, thread: null, options: null, cancellationToken: cancellationToken);

        _logger.LogInformation("Agent '{AgentName}' response: {Response}", _agent.Name, response);
    }

    private async Task RunWithDevUIAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting DevUI for agent '{AgentName}' on port {Port}...", _agent.Name, _providerSettings.DevUIPort);

        var builder = WebApplication.CreateBuilder();
        
        // Configure logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        // Register this agent with DevUI
        builder.Services.AddSingleton(_agent);
        builder.AddDevUI();
        builder.AddOpenAIResponses();
        builder.AddOpenAIConversations();

        var app = builder.Build();
        
        app.MapOpenAIResponses();
        app.MapOpenAIConversations();
        app.MapDevUI();

        var url = $"http://localhost:{_providerSettings.DevUIPort}";
        app.Urls.Add(url);

        await app.StartAsync(cancellationToken);

        var devUIUrl = $"{url}/devui";
        _logger.LogInformation("DevUI available at: {DevUIUrl}", devUIUrl);
        
        // Open browser
        Process.Start(new ProcessStartInfo(devUIUrl) { UseShellExecute = true });

        _logger.LogInformation("Press Ctrl+C to stop DevUI...");
        
        // Block until cancellation
        await app.WaitForShutdownAsync(cancellationToken);
    }
}
