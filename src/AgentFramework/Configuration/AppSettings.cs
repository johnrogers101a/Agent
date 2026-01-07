using System.Reflection;

namespace AgentFramework.Configuration;

public class AppSettings
{
    public ProviderSettings Provider { get; set; } = new();
    public ClientsSettings Clients { get; set; } = new();
    public List<AgentSettings> Agents { get; set; } = [];

    public static AppSettings LoadConfiguration(string fileName = "appsettings.json")
    {
        var builder = new ConfigurationBuilder()
            .AddJsonFile(fileName);

        // Try to load user secrets from the entry assembly (the main app)
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly != null)
        {
            builder.AddUserSecrets(entryAssembly, optional: true);
        }

        return builder.Build().Get<AppSettings>()!;
    }

    public class ProviderSettings
    {
        public string Type { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public string? ApiKey { get; set; }
        public bool DevUI { get; set; } = false;
        public int DevUIPort { get; set; } = 8080;
    }

    public class ClientsSettings
    {
        public WeatherSettings Weather { get; set; } = new();
        public GmailSettings Gmail { get; set; } = new();
    }

    public class WeatherSettings
    {
        public string ApiKey { get; set; } = string.Empty;
    }

    public class GmailSettings
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
    }

    public class AgentSettings
    {
        public string Name { get; set; } = string.Empty;
        public string InstructionsFileName { get; set; } = string.Empty;
        public List<string> Tools { get; set; } = [];
        /// <summary>
        /// Optional per-tool description overrides. Key is tool name, value is file path to additional description.
        /// </summary>
        public Dictionary<string, string>? ToolDescriptions { get; set; }
    }
}
