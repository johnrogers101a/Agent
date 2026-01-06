using System.Reflection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using AgentFramework.Configuration;
using AgentFramework.Core;
using static AgentFramework.Constants;

namespace AgentFramework.Tools;

public class ToolRegistry
{
    private readonly Dictionary<string, AITool> _tools = new();
    private readonly ToolDescriptionLoader _descriptionLoader;
    private readonly ILogger<ToolRegistry> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public ToolRegistry(ILogger<ToolRegistry> logger, ILogger<ToolDescriptionLoader> descriptionLoaderLogger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _descriptionLoader = new ToolDescriptionLoader(descriptionLoaderLogger);
    }

    public void Register(string name, AITool tool)
    {
        _logger.LogDebug("Registering tool: {ToolName}", name);
        _tools[name] = tool;
    }

    public IList<AITool> GetTools(IEnumerable<string> toolNames)
    {
        var names = toolNames.ToList();
        _logger.LogDebug("GetTools called for: {ToolNames}", string.Join(", ", names));
        var result = names
            .Where(name => _tools.ContainsKey(name))
            .Select(name => _tools[name])
            .ToList();
        _logger.LogDebug("Returning {ToolCount} tools", result.Count);
        return result;
    }

    /// <summary>
    /// Loads and registers tools using reflection based on configuration.
    /// Validates all tool description files at startup.
    /// </summary>
    public void LoadFromConfiguration(AppSettings appSettings)
    {
        _logger.LogInformation("========================================");
        _logger.LogInformation("LoadFromConfiguration starting...");
        _logger.LogInformation("Working directory: {WorkingDirectory}", Directory.GetCurrentDirectory());
        _logger.LogInformation("Tools count in config: {ToolCount}", appSettings.Tools.Count);
        
        // Validate all tool description files upfront
        _logger.LogDebug("Calling ValidateAll...");
        _descriptionLoader.ValidateAll(appSettings.Tools);
        _logger.LogDebug("ValidateAll completed");

        // Register tools using reflection
        _logger.LogDebug("Registering tools...");
        foreach (var toolConfig in appSettings.Tools)
        {
            _logger.LogDebug("Creating tool: {ToolName}", toolConfig.Name);
            _logger.LogTrace("  Class: {Class}", toolConfig.Class);
            _logger.LogTrace("  FactoryMethod: {FactoryMethod}", toolConfig.FactoryMethod);
            _logger.LogTrace("  BaseDescription: {BaseDescription}", toolConfig.BaseDescription);
            _logger.LogTrace("  Description: {Description}", toolConfig.Description ?? "(none)");
            
            var tool = CreateToolFromConfig(toolConfig);
            if (tool != null)
            {
                Register(toolConfig.Name, tool);
                _logger.LogDebug("  Tool registered successfully");
            }
            else
            {
                _logger.LogWarning("  Tool creation returned null for {ToolName}", toolConfig.Name);
            }
        }
        
        _logger.LogInformation("LoadFromConfiguration complete. Total tools registered: {ToolCount}", _tools.Count);
        _logger.LogInformation("Registered tools: {ToolNames}", string.Join(", ", _tools.Keys));
        _logger.LogInformation("========================================");
    }

    private AITool? CreateToolFromConfig(AppSettings.ToolSettings toolConfig)
    {
        try
        {
            // Get the type from the assembly-qualified class name
            _logger.LogTrace("  Resolving type: {Class}", toolConfig.Class);
            var type = Type.GetType(toolConfig.Class);
            if (type == null)
            {
                _logger.LogError(ErrorMessages.TypeNotFound, toolConfig.Class, toolConfig.Name);
                return null;
            }
            _logger.LogTrace("  Type resolved: {TypeFullName}", type.FullName);

            // Load description from markdown files (base + optional extension)
            _logger.LogTrace("  Loading description...");
            var description = _descriptionLoader.Load(toolConfig);
            _logger.LogDebug("  Description loaded, length: {Length} chars", description.Length);
            
            // Try to find method with (string, ILoggerFactory?) parameters
            _logger.LogTrace("  Looking for method: {FactoryMethod}(string, ILoggerFactory?)", toolConfig.FactoryMethod);
            var method = type.GetMethod(toolConfig.FactoryMethod, BindingFlags.Public | BindingFlags.Static, [typeof(string), typeof(ILoggerFactory)]);
            
            if (method != null)
            {
                _logger.LogTrace("  Found method with (string, ILoggerFactory?) parameters, invoking...");
                var result = method.Invoke(null, [description, _loggerFactory]) as AITool;
                if (result != null)
                {
                    _logger.LogDebug("  AITool created successfully");
                    var aiFunction = result as AIFunction;
                    _logger.LogTrace("  AITool.Name: {Name}", aiFunction?.Name ?? "N/A");
                    _logger.LogTrace("  AITool.Description: {Description}", aiFunction?.Description ?? "N/A");
                }
                return result;
            }
            
            // Try to find method with string parameter only (for description)
            _logger.LogTrace("  Looking for method: {FactoryMethod}(string)", toolConfig.FactoryMethod);
            method = type.GetMethod(toolConfig.FactoryMethod, BindingFlags.Public | BindingFlags.Static, [typeof(string)]);
            
            if (method != null)
            {
                _logger.LogTrace("  Found method with string parameter, invoking...");
                // Invoke with description from markdown
                var result = method.Invoke(null, [description]) as AITool;
                if (result != null)
                {
                    _logger.LogDebug("  AITool created successfully");
                    var aiFunction = result as AIFunction;
                    _logger.LogTrace("  AITool.Name: {Name}", aiFunction?.Name ?? "N/A");
                    _logger.LogTrace("  AITool.Description: {Description}", aiFunction?.Description ?? "N/A");
                }
                return result;
            }

            // Fall back to parameterless method
            _logger.LogTrace("  Method with string param not found, looking for parameterless...");
            method = type.GetMethod(toolConfig.FactoryMethod, BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes);
            
            if (method != null)
            {
                _logger.LogTrace("  Found parameterless method, invoking...");
                return method.Invoke(null, null) as AITool;
            }

            _logger.LogError(ErrorMessages.FactoryMethodNotFound, toolConfig.FactoryMethod, toolConfig.Class, toolConfig.Name);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception creating tool {ToolName}: {Message}", toolConfig.Name, ex.Message);
            return null;
        }
    }
}
