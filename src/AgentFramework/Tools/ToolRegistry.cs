using Microsoft.Extensions.AI;

namespace AgentFramework.Tools;

public class ToolRegistry
{
    private readonly Dictionary<string, AITool> _tools = new();

    public void Register(string name, AITool tool)
    {
        _tools[name] = tool;
    }

    public IList<AITool> GetTools(IEnumerable<string> toolNames)
    {
        return toolNames
            .Where(name => _tools.ContainsKey(name))
            .Select(name => _tools[name])
            .ToList();
    }
}
