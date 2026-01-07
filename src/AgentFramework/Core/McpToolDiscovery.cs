#nullable enable

using AgentFramework.Attributes;
using Microsoft.Extensions.AI;
using System.Reflection;
using System.Xml.Linq;

namespace AgentFramework.Core;

/// <summary>
/// Parsed tool description containing function and parameter descriptions from XML comments.
/// </summary>
public class ToolDescription
{
    public string FunctionDescription { get; set; } = string.Empty;
    public Dictionary<string, string> ParameterDescriptions { get; set; } = new();
}

/// <summary>
/// Represents a discovered tool with its method info and base description.
/// </summary>
public class DiscoveredTool
{
    public required string Name { get; init; }
    public required MethodInfo Method { get; init; }
    public required Type DeclaringType { get; init; }
    public required Assembly Assembly { get; init; }
    public ToolDescription? Description { get; init; }
    public object? Instance { get; init; }
}

/// <summary>
/// Discovers and registers MCP tools from loaded assemblies using XML documentation comments.
/// </summary>
public static class McpToolDiscovery
{
    private static Dictionary<string, DiscoveredTool>? _discoveredTools;
    private static readonly Dictionary<string, XDocument?> _xmlDocCache = new();
    private static readonly object _lock = new();

    /// <summary>
    /// Discovers all tools marked with [McpTool] attribute across all loaded assemblies.
    /// Results are cached after first discovery.
    /// </summary>
    public static Dictionary<string, DiscoveredTool> DiscoverAllTools()
    {
        if (_discoveredTools is not null)
            return _discoveredTools;

        lock (_lock)
        {
            if (_discoveredTools is not null)
                return _discoveredTools;

            _discoveredTools = new Dictionary<string, DiscoveredTool>(StringComparer.OrdinalIgnoreCase);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Skip system assemblies
                var name = assembly.GetName().Name;
                if (name is null || 
                    name.StartsWith("System", StringComparison.Ordinal) || 
                    name.StartsWith("Microsoft", StringComparison.Ordinal) ||
                    name.StartsWith("netstandard", StringComparison.Ordinal))
                    continue;

                Console.WriteLine($"[DEBUG] Scanning assembly: {name}");
                DiscoverToolsInAssembly(assembly, _discoveredTools);
            }

            return _discoveredTools;
        }
    }

    /// <summary>
    /// Forces re-discovery of all tools. Useful after loading new assemblies.
    /// </summary>
    public static void Reset()
    {
        lock (_lock)
        {
            _discoveredTools = null;
        }
    }

    /// <summary>
    /// Creates AIFunction instances for the specified tool names.
    /// </summary>
    /// <param name="toolNames">List of tool names to create.</param>
    /// <param name="toolDescriptionOverrides">Optional per-tool description overrides (file paths).</param>
    /// <param name="basePath">Base path for resolving description override files.</param>
    /// <param name="serviceProvider">Optional service provider for DI-based tool instantiation.</param>
    public static List<AIFunction> CreateToolsForAgent(
        List<string> toolNames,
        Dictionary<string, string>? toolDescriptionOverrides = null,
        string? basePath = null,
        IServiceProvider? serviceProvider = null)
    {
        var allTools = DiscoverAllTools();
        var functions = new List<AIFunction>();

        foreach (var toolName in toolNames)
        {
            if (!allTools.TryGetValue(toolName, out var tool))
            {
                throw new InvalidOperationException(
                    $"Tool '{toolName}' not found. Available tools: {string.Join(", ", allTools.Keys)}");
            }

            var description = tool.Description?.FunctionDescription ?? string.Empty;

            // Append agent-specific description if provided
            if (toolDescriptionOverrides?.TryGetValue(toolName, out var overridePath) == true)
            {
                var fullPath = basePath is not null 
                    ? Path.Combine(basePath, overridePath) 
                    : overridePath;

                if (File.Exists(fullPath))
                {
                    var additionalDescription = File.ReadAllText(fullPath);
                    if (!string.IsNullOrWhiteSpace(additionalDescription))
                    {
                        description = string.IsNullOrEmpty(description)
                            ? additionalDescription
                            : $"{description}\n\n---\n\n{additionalDescription}";
                    }
                }
            }

            var aiFunction = CreateAIFunction(tool, description, serviceProvider);
            functions.Add(aiFunction);
        }

        return functions;
    }

    /// <summary>
    /// Gets all discovered tool names.
    /// </summary>
    public static IEnumerable<string> GetAvailableToolNames()
    {
        return DiscoverAllTools().Keys;
    }

    private static void DiscoverToolsInAssembly(Assembly assembly, Dictionary<string, DiscoveredTool> tools)
    {
        try
        {
            // Pre-load XML documentation for this assembly
            var xmlDoc = LoadXmlDocumentation(assembly);

            foreach (var type in assembly.GetExportedTypes())
            {
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
                {
                    var attribute = method.GetCustomAttribute<McpToolAttribute>();
                    if (attribute is null)
                        continue;

                    // Use attribute name, or class name, or method name as fallback
                    var toolName = attribute.Name ?? type.Name;
                    var description = GetMethodDescription(xmlDoc, method);

                    var tool = new DiscoveredTool
                    {
                        Name = toolName,
                        Method = method,
                        DeclaringType = type,
                        Assembly = assembly,
                        Description = description,
                        Instance = method.IsStatic ? null : CreateInstance(type)
                    };

                    if (tools.ContainsKey(toolName))
                    {
                        throw new InvalidOperationException(
                            $"Duplicate tool name '{toolName}' found in {type.FullName}.{method.Name}. " +
                            $"Tool names must be unique across all assemblies.");
                    }

                    tools[toolName] = tool;
                }
            }
        }
        catch (ReflectionTypeLoadException)
        {
            // Skip assemblies that can't be loaded
        }
    }

    /// <summary>
    /// Loads the XML documentation file for an assembly.
    /// </summary>
    private static XDocument? LoadXmlDocumentation(Assembly assembly)
    {
        var assemblyLocation = assembly.Location;
        if (string.IsNullOrEmpty(assemblyLocation))
            return null;

        // Check cache first
        if (_xmlDocCache.TryGetValue(assemblyLocation, out var cached))
            return cached;

        // Try to find the XML file next to the assembly
        var xmlPath = Path.ChangeExtension(assemblyLocation, ".xml");
        
        XDocument? doc = null;
        if (File.Exists(xmlPath))
        {
            try
            {
                doc = XDocument.Load(xmlPath);
            }
            catch
            {
                // Ignore XML parsing errors
            }
        }

        _xmlDocCache[assemblyLocation] = doc;
        return doc;
    }

    /// <summary>
    /// Extracts the summary and parameter descriptions from XML documentation for a method.
    /// </summary>
    private static ToolDescription? GetMethodDescription(XDocument? xmlDoc, MethodInfo method)
    {
        if (xmlDoc is null)
            return null;

        // Build the member name in XML doc format: M:Namespace.Type.Method(ParamType1,ParamType2)
        var memberName = GetXmlMemberName(method);

        var memberElement = xmlDoc.Descendants("member")
            .FirstOrDefault(m => m.Attribute("name")?.Value == memberName);

        if (memberElement is null)
            return null;

        var result = new ToolDescription();

        // Get summary
        var summary = memberElement.Element("summary")?.Value;
        if (!string.IsNullOrWhiteSpace(summary))
        {
            result.FunctionDescription = CleanXmlText(summary);
        }

        // Get parameter descriptions
        foreach (var paramElement in memberElement.Elements("param"))
        {
            var paramName = paramElement.Attribute("name")?.Value;
            var paramDesc = paramElement.Value;
            if (!string.IsNullOrWhiteSpace(paramName) && !string.IsNullOrWhiteSpace(paramDesc))
            {
                result.ParameterDescriptions[paramName] = CleanXmlText(paramDesc);
            }
        }

        return result;
    }

    /// <summary>
    /// Builds the XML documentation member name for a method.
    /// Format: M:Namespace.Type.Method(ParamType1,ParamType2)
    /// </summary>
    private static string GetXmlMemberName(MethodInfo method)
    {
        var typeName = method.DeclaringType?.FullName?.Replace('+', '.') ?? "";
        var methodName = method.Name;
        var parameters = method.GetParameters();

        if (parameters.Length == 0)
        {
            return $"M:{typeName}.{methodName}";
        }

        var paramTypes = string.Join(",", parameters.Select(p => GetXmlTypeName(p.ParameterType)));
        return $"M:{typeName}.{methodName}({paramTypes})";
    }

    /// <summary>
    /// Gets the XML documentation type name for a parameter type.
    /// </summary>
    private static string GetXmlTypeName(Type type)
    {
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            var genericArgs = string.Join(",", type.GetGenericArguments().Select(GetXmlTypeName));
            var baseName = genericDef.FullName?[..genericDef.FullName.IndexOf('`')] ?? genericDef.Name;
            return $"{baseName}{{{genericArgs}}}";
        }

        if (type.IsArray)
        {
            return GetXmlTypeName(type.GetElementType()!) + "[]";
        }

        return type.FullName?.Replace('+', '.') ?? type.Name;
    }

    /// <summary>
    /// Cleans whitespace from XML text content.
    /// </summary>
    private static string CleanXmlText(string text)
    {
        // Normalize whitespace: collapse multiple spaces/newlines into single space
        var lines = text.Split('\n')
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l));
        return string.Join(" ", lines);
    }

    private static AIFunction CreateAIFunction(DiscoveredTool tool, string description, IServiceProvider? serviceProvider = null)
    {
        // Build options - AIFunctionFactory will automatically pick up parameter metadata from the method
        var options = new AIFunctionFactoryOptions
        {
            Name = tool.Name,
            Description = description
        };

        // For static methods, we can pass null as the target
        // For instance methods, we need an instance
        if (tool.Method.IsStatic)
        {
            return AIFunctionFactory.Create(tool.Method, target: null, options);
        }
        else
        {
            var instance = tool.Instance ?? CreateInstance(tool.DeclaringType, serviceProvider);
            if (instance is null)
            {
                throw new InvalidOperationException(
                    $"Cannot create instance of tool '{tool.Name}' (type: {tool.DeclaringType.FullName}). " +
                    $"Ensure all constructor dependencies are registered in DI or use a static method.");
            }
            return AIFunctionFactory.Create(tool.Method, target: instance, options);
        }
    }

    private static object? CreateInstance(Type type, IServiceProvider? serviceProvider = null)
    {
        // First try DI if available
        if (serviceProvider is not null)
        {
            try
            {
                return ActivatorUtilities.CreateInstance(serviceProvider, type);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] ActivatorUtilities failed for {type.Name}: {ex.Message}");
                // Fall through to other methods
            }
        }

        // Try to get a parameterless constructor
        var constructor = type.GetConstructor(Type.EmptyTypes);
        if (constructor is not null)
        {
            return Activator.CreateInstance(type);
        }

        // For types without parameterless constructors, return null
        // (they should use static methods for tools)
        return null;
    }
}
