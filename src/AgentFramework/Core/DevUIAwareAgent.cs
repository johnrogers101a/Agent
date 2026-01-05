using Microsoft.Extensions.Logging;
using Microsoft.Agents.AI;
using static AgentFramework.Configuration.AppSettings;

namespace AgentFramework.Core;

/// <summary>
/// Wraps an AIAgent with logging and settings context.
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

    /// <summary>
    /// Gets the underlying AIAgent instance.
    /// </summary>
    public AIAgent Agent => _agent;

    /// <summary>
    /// Gets the provider settings.
    /// </summary>
    public ProviderSettings Settings => _providerSettings;

    /// <summary>
    /// Runs the agent with the given prompt and returns the response.
    /// </summary>
    public async Task<string?> RunAsync(string prompt, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Agent '{AgentName}' processing: {Prompt}", _agent.Name, prompt);

        var result = await _agent.RunAsync(prompt, thread: null, options: null, cancellationToken: cancellationToken);

        // AgentRunResponse can be converted to string
        var response = result?.ToString();
        _logger.LogInformation("Agent '{AgentName}' response: {Response}", _agent.Name, response);
        
        return response;
    }
}
