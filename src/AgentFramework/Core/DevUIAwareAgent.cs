using Microsoft.Extensions.Logging;
using Microsoft.Agents.AI;
using static AgentFramework.Configuration.AppSettings;

namespace AgentFramework.Core;

/// <summary>
/// Wraps an AIAgent with logging, settings context, and conversation memory.
/// </summary>
public class DevUIAwareAgent
{
    private readonly AIAgent _agent;
    private readonly ConversationMemory? _memory;
    private readonly ProviderSettings _providerSettings;
    private readonly ILogger _logger;

    public DevUIAwareAgent(
        AIAgent agent,
        ConversationMemory? memory,
        ProviderSettings providerSettings,
        ILogger logger)
    {
        _agent = agent;
        _memory = memory;
        _providerSettings = providerSettings;
        _logger = logger;
    }

    /// <summary>
    /// Gets the underlying AIAgent instance.
    /// </summary>
    public AIAgent Agent => _agent;

    /// <summary>
    /// Gets the conversation memory, if available.
    /// </summary>
    public ConversationMemory? Memory => _memory;

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

    /// <summary>
    /// Resets the conversation memory, optionally preserving a summary.
    /// </summary>
    /// <param name="preserveSummary">If true, the conversation will be summarized before clearing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ResetConversationAsync(bool preserveSummary = true, CancellationToken cancellationToken = default)
    {
        if (_memory is not null)
        {
            _logger.LogInformation("Agent '{AgentName}' resetting conversation (preserveSummary: {PreserveSummary})", 
                _agent.Name, preserveSummary);
            await _memory.ResetConversationAsync(preserveSummary, cancellationToken);
        }
    }

    /// <summary>
    /// Clears all conversation history immediately without summarization.
    /// </summary>
    public void ClearConversation()
    {
        if (_memory is not null)
        {
            _logger.LogInformation("Agent '{AgentName}' clearing conversation history", _agent.Name);
            _memory.ClearHistory();
        }
    }
}
