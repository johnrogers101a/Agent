#nullable enable

namespace AgentFramework.Attributes;

/// <summary>
/// Marks a method as an MCP tool that can be discovered and registered automatically.
/// The tool description is loaded from an embedded resource named {ToolName}.md.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class McpToolAttribute : Attribute
{
    /// <summary>
    /// Optional tool name override. If not specified, the method name is used.
    /// </summary>
    public string? Name { get; set; }

    public McpToolAttribute() { }

    public McpToolAttribute(string name)
    {
        Name = name;
    }
}
