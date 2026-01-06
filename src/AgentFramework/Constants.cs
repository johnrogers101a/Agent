namespace AgentFramework;

/// <summary>
/// Constants used across the AgentFramework.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Chat provider type identifiers.
    /// </summary>
    public static class ProviderTypes
    {
        public const string Ollama = "Ollama";
        public const string AzureFoundry = "AzureFoundry";
    }

    /// <summary>
    /// Chat message roles.
    /// </summary>
    public static class Roles
    {
        public const string User = "user";
        public const string Assistant = "assistant";
        public const string System = "system";
    }

    /// <summary>
    /// Ollama API constants.
    /// </summary>
    public static class OllamaApi
    {
        // Routes
        public const string GenerateRoute = "/api/generate";
        public const string ChatRoute = "/api/chat";
        public const string TagsRoute = "/api/tags";
        public const string PsRoute = "/api/ps";
        public const string ResetRoute = "/api/reset";
        public const string ShowRoute = "/api/show";

        // Content types
        public const string NdjsonContentType = "application/x-ndjson";

        // Response values
        public const string DoneReasonStop = "stop";
        
        // Model details
        public const string FormatGguf = "gguf";
        public const string FamilyAgent = "agent";
        public const string NotApplicable = "N/A";
        
        // Digest prefix
        public const string Sha256Prefix = "sha256:";
    }

    /// <summary>
    /// Prompt formatting constants.
    /// </summary>
    public static class PromptFormat
    {
        public const string UserPrefix = "User: ";
        public const string AssistantPrefix = "Assistant: ";
        public const string SystemPrefix = "System: ";
    }

    /// <summary>
    /// Error message templates.
    /// </summary>
    public static class ErrorMessages
    {
        public const string ModelNotFound = "Model '{0}' not found";
        public const string ErrorProcessingRequest = "Error processing request: {0}";
        public const string ProviderNotSupported = "Provider '{0}' is not supported.";
        public const string TypeNotFound = "Warning: Could not find type '{0}' for tool '{1}'";
        public const string FactoryMethodNotFound = "Warning: Could not find factory method '{0}' on type '{1}' for tool '{2}'";
        public const string ErrorCreatingTool = "Error creating tool '{0}': {1}";
    }

    /// <summary>
    /// Startup and informational message templates.
    /// </summary>
    public static class StartupMessages
    {
        public const string DevUIAvailable = "DevUI available at: {0}";
        public const string OllamaApiRunning = "PersonalAgent Ollama-compatible API running at http://localhost:{0}";
        public const string AvailableEndpoints = "Available endpoints:";
        public const string EndpointGenerate = "  POST /api/generate - Generate completion";
        public const string EndpointChat = "  POST /api/chat     - Chat completion";
        public const string EndpointTags = "  GET  /api/tags     - List models";
        public const string EndpointPs = "  GET  /api/ps       - List running models";
        public const string EndpointReset = "  POST /api/reset    - Reset conversation memory";
        public const string EndpointShow = "  POST /api/show     - Show model info";
    }
}
