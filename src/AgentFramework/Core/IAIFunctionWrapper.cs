#nullable enable

using Microsoft.Extensions.AI;

namespace AgentFramework.Core;

/// <summary>
/// Extension point for wrapping AIFunction instances with additional behavior.
/// Implementations can add logging, durable activity scheduling, caching, etc.
/// </summary>
public interface IAIFunctionWrapper
{
    /// <summary>
    /// Wraps an AIFunction with additional behavior.
    /// </summary>
    /// <param name="function">The original AIFunction to wrap.</param>
    /// <param name="tool">The discovered tool metadata.</param>
    /// <returns>A wrapped AIFunction (may be the same instance if no wrapping needed).</returns>
    AIFunction Wrap(AIFunction function, DiscoveredTool tool);
}
