using System.Reflection;
using Microsoft.Extensions.AI;
using AgentFramework.Configuration;

namespace AgentFramework.Tools;

public class ToolRegistry
{
    private readonly Dictionary<string, AITool> _tools = new();
    private readonly Dictionary<string, string> _toolDescriptions = new();

    public void Register(string name, AITool tool)
    {
        _tools[name] = tool;
    }

    public void RegisterDescription(string name, string description)
    {
        _toolDescriptions[name] = description;
    }

    public string? GetDescription(string name)
    {
        return _toolDescriptions.TryGetValue(name, out var desc) ? desc : null;
    }

    public IList<AITool> GetTools(IEnumerable<string> toolNames)
    {
        return toolNames
            .Where(name => _tools.ContainsKey(name))
            .Select(name => _tools[name])
            .ToList();
    }

    /// <summary>
    /// Loads tool descriptions from markdown files in the specified folder.
    /// Each .md file should be named after the tool (e.g., GetWeatherByZip.md).
    /// </summary>
    public void LoadDescriptionsFromFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            return;

        foreach (var file in Directory.GetFiles(folderPath, "*.md"))
        {
            var toolName = Path.GetFileNameWithoutExtension(file);
            var description = File.ReadAllText(file);
            RegisterDescription(toolName, description);
        }
    }

    /// <summary>
    /// Loads and registers tools using reflection based on configuration.
    /// </summary>
    public void LoadFromConfiguration(AppSettings appSettings)
    {
        // Load descriptions from markdown files first
        LoadDescriptionsFromFolder(appSettings.ToolsFolder);

        // Register tools using reflection
        foreach (var toolConfig in appSettings.Tools)
        {
            var tool = CreateToolFromConfig(toolConfig);
            if (tool != null)
            {
                Register(toolConfig.Name, tool);
            }
        }
    }

    private AITool? CreateToolFromConfig(AppSettings.ToolSettings toolConfig)
    {
        try
        {
            // Get the type from the assembly-qualified class name
            var type = Type.GetType(toolConfig.Class);
            if (type == null)
            {
                Console.WriteLine($"Warning: Could not find type '{toolConfig.Class}' for tool '{toolConfig.Name}'");
                return null;
            }

            // Find the factory method - try with description parameter first, then without
            var description = GetDescription(toolConfig.Name);
            
            // Try to find method with string? parameter (for description)
            var method = type.GetMethod(toolConfig.FactoryMethod, BindingFlags.Public | BindingFlags.Static, [typeof(string)]);
            
            if (method != null)
            {
                // Invoke with description from markdown
                return method.Invoke(null, [description]) as AITool;
            }

            // Fall back to parameterless method
            method = type.GetMethod(toolConfig.FactoryMethod, BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes);
            
            if (method != null)
            {
                return method.Invoke(null, null) as AITool;
            }

            Console.WriteLine($"Warning: Could not find factory method '{toolConfig.FactoryMethod}' on type '{toolConfig.Class}' for tool '{toolConfig.Name}'");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating tool '{toolConfig.Name}': {ex.Message}");
            return null;
        }
    }
}
