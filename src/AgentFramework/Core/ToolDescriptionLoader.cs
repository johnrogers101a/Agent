using Microsoft.Extensions.Logging;
using AgentFramework.Configuration;
using static AgentFramework.Constants;

namespace AgentFramework.Core;

/// <summary>
/// Loads and validates tool descriptions from markdown files.
/// Supports layered descriptions: base description + optional agent-specific extension.
/// </summary>
public class ToolDescriptionLoader
{
    private const string DescriptionSeparator = "\r\n----------------------------------------------\r\n";
    private readonly ILogger<ToolDescriptionLoader> _logger;

    public ToolDescriptionLoader(ILogger<ToolDescriptionLoader> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates all tool description files exist and are readable.
    /// Throws InvalidOperationException with all errors if any files are missing or corrupt.
    /// </summary>
    public void ValidateAll(IEnumerable<AppSettings.ToolSettings> tools)
    {
        _logger.LogInformation("Starting validation of tool description files...");
        var errors = new List<string>();
        var toolList = tools.ToList();
        _logger.LogInformation("Found {ToolCount} tools to validate", toolList.Count);

        foreach (var tool in toolList)
        {
            _logger.LogDebug("Validating tool: {ToolName}", tool.Name);
            _logger.LogTrace("  BaseDescription: {BaseDescription}", tool.BaseDescription ?? "(null)");
            _logger.LogTrace("  Description: {Description}", tool.Description ?? "(null)");

            // Validate BaseDescription (required)
            if (string.IsNullOrWhiteSpace(tool.BaseDescription))
            {
                var error = string.Format(ErrorMessages.ToolBaseDescriptionMissing, tool.Name);
                _logger.LogError("Tool {ToolName}: {Error}", tool.Name, error);
                errors.Add(error);
            }
            else
            {
                var absolutePath = Path.GetFullPath(tool.BaseDescription);
                _logger.LogTrace("  BaseDescription absolute path: {AbsolutePath}", absolutePath);
                _logger.LogTrace("  BaseDescription exists: {Exists}", File.Exists(absolutePath));
                
                var baseError = ValidateFile(tool.BaseDescription, tool.Name, isBase: true);
                if (baseError != null)
                {
                    _logger.LogError("Tool {ToolName}: {Error}", tool.Name, baseError);
                    errors.Add(baseError);
                }
                else
                {
                    _logger.LogDebug("  BaseDescription validated OK");
                }
            }

            // Validate Description (optional, but if specified must be valid)
            if (!string.IsNullOrWhiteSpace(tool.Description))
            {
                var absolutePath = Path.GetFullPath(tool.Description);
                _logger.LogTrace("  Description absolute path: {AbsolutePath}", absolutePath);
                _logger.LogTrace("  Description exists: {Exists}", File.Exists(absolutePath));
                
                var descError = ValidateFile(tool.Description, tool.Name, isBase: false);
                if (descError != null)
                {
                    _logger.LogError("Tool {ToolName}: {Error}", tool.Name, descError);
                    errors.Add(descError);
                }
                else
                {
                    _logger.LogDebug("  Description validated OK");
                }
            }
        }

        if (errors.Count > 0)
        {
            _logger.LogError("Validation FAILED with {ErrorCount} errors", errors.Count);
            foreach (var error in errors)
            {
                _logger.LogError("  - {Error}", error);
            }
            throw new InvalidOperationException(
                string.Format(ErrorMessages.ToolDescriptionValidationFailed, string.Join(Environment.NewLine, errors)));
        }

        _logger.LogInformation("Validation completed successfully");
    }

    private string? ValidateFile(string filePath, string toolName, bool isBase)
    {
        if (!File.Exists(filePath))
        {
            return string.Format(
                isBase ? ErrorMessages.ToolBaseDescriptionFileNotFound : ErrorMessages.ToolDescriptionFileNotFound,
                toolName,
                filePath);
        }

        try
        {
            // Attempt to read the file to ensure it's not corrupt
            var content = File.ReadAllText(filePath);
            _logger.LogTrace("  File read OK, length: {Length} chars", content.Length);
            return null;
        }
        catch (Exception ex)
        {
            return string.Format(ErrorMessages.ToolDescriptionFileUnreadable, toolName, filePath, ex.Message);
        }
    }

    /// <summary>
    /// Loads the combined description for a tool.
    /// Returns base description, optionally appended with extension description using a separator.
    /// </summary>
    public string Load(AppSettings.ToolSettings tool)
    {
        _logger.LogDebug("Loading description for tool: {ToolName}", tool.Name);
        
        var baseDescription = File.ReadAllText(tool.BaseDescription);
        _logger.LogDebug("  Base description loaded ({Length} chars)", baseDescription.Length);
        _logger.LogTrace("  ----BASE START----\n{BaseDescription}\n  ----BASE END----", baseDescription);

        if (string.IsNullOrWhiteSpace(tool.Description))
        {
            _logger.LogDebug("  No extension description, returning base only");
            return baseDescription;
        }

        var extensionDescription = File.ReadAllText(tool.Description);
        _logger.LogDebug("  Extension description loaded ({Length} chars)", extensionDescription.Length);
        _logger.LogTrace("  ----EXTENSION START----\n{ExtensionDescription}\n  ----EXTENSION END----", extensionDescription);

        var combined = baseDescription + DescriptionSeparator + extensionDescription;
        _logger.LogDebug("  Combined description ({Length} chars)", combined.Length);
        _logger.LogTrace("  ----COMBINED START----\n{CombinedDescription}\n  ----COMBINED END----", combined);
        
        return combined;
    }
}
