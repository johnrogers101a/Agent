# AI Coding Agent Instructions

## Architecture Overview

This is a .NET 10 AI Agent framework built on [Microsoft Agent Framework](https://github.com/microsoft/agent-framework) (`Microsoft.Agents.AI` SDK), designed for deployment to [Microsoft Foundry](https://learn.microsoft.com/en-us/azure/ai-foundry/what-is-foundry).

### Multi-Project Structure

```
Agent.sln
├── src/AgentFramework/     # Core framework library (Microsoft.Agents.AI, DevUI, OllamaSharp)
│   ├── Core/               # AgentFactory, DevUIAwareAgent, McpToolDiscovery
│   ├── Attributes/         # McpToolAttribute
│   ├── Extensions/         # ConfigureAgent(), UseAgents()
│   └── Infrastructure/     # ChatClientProviderFactory, Providers/
├── src/PersonalAgent/      # Example agent application
│   ├── Program.cs          # 4 lines of code
│   ├── appsettings.json    # Agent configuration
│   └── Agents/             # Markdown instruction files
├── src/Gmail/              # Gmail tools library
│   └── Tools/              # GetMail, SearchMail, GetMailContents
└── src/Weather/            # Weather tools library
    └── Tools/              # GetWeatherByZip, GetDailyForecastByZip, etc.
```

### Key Architectural Decisions

- **4-line Program.cs** using `ConfigureAgent()` and `UseAgents()` extension methods
- **Declarative agent config** in `appsettings.json`, not code
- **Agent instructions** in markdown files under `Agents/` folder
- **Automatic tool discovery** via `[McpTool]` attribute — no manual registration
- **XML documentation** provides tool and parameter descriptions
- **DI-based tools** with constructor injection for `HttpClient`, `IConfiguration`, `ILogger`, etc.
- **OpenAI-compatible API** following [Ollama API spec](https://docs.ollama.com/api/openai-compatibility)

## Adding New Capabilities

### Adding a New Tool

1. Create a tool class in a `Tools/` folder (in any referenced project):
   - Use XML doc comments for class and method descriptions
   - Mark the execution method with `[McpTool]` attribute
   - Use constructor injection for dependencies
   - Return a typed response object

2. Reference the tool's project from your agent application

3. Add the tool name to the agent's `Tools` array in `appsettings.json`

**Example tool:**
```csharp
/// <summary>
/// Gets current weather for a US zip code using Google Weather API.
/// </summary>
public class GetWeatherByZip
{
    private readonly HttpClient _http;
    private readonly ILogger<GetWeatherByZip> _logger;
    private readonly string _apiKey;

    public GetWeatherByZip(HttpClient httpClient, IConfiguration config, ILogger<GetWeatherByZip> logger)
    {
        _http = httpClient;
        _logger = logger;
        _apiKey = config["Clients:Weather:ApiKey"]!;
    }

    /// <summary>
    /// Gets current weather for a US zip code.
    /// </summary>
    /// <param name="zipCode">US zip code (e.g., 98052).</param>
    [McpTool]
    public async Task<CurrentWeatherResponse> ExecuteAsync(string zipCode)
    {
        // Implementation here
    }
}
```

### Adding External API Clients

1. Create a tools library project (e.g., `src/MyService/`)
2. Add tool classes with `[McpTool]` methods
3. Add configuration section to `AppSettings.cs` under `ClientsSettings`
4. Reference the project from your agent application

### Adding a New LLM Provider

1. Implement `IChatClientProvider` in `Infrastructure/Providers/`
2. Register in `ChatClientProviderFactory._providers` dictionary
3. Set `Provider.Type` in `appsettings.json`

Current providers: `Ollama` (local), `AzureFoundry` (Azure OpenAI with CLI or API key auth)

## Configuration

All runtime config is in `appsettings.json`:

```json
{
  "Provider": {
    "Type": "Ollama",
    "Endpoint": "http://localhost:11434",
    "ModelName": "llama3.2:latest",
    "DevUI": true,
    "DevUIPort": 8080
  },
  "Clients": {
    "Weather": { "ApiKey": "..." },
    "Gmail": { "ClientId": "...", "ClientSecret": "..." }
  },
  "Agents": [
    {
      "Name": "Personal",
      "InstructionsFileName": "Agents/personal.md",
      "Tools": ["GetWeatherByZip", "GetMail", "SearchMail"]
    }
  ]
}
```

## Runtime Modes

### DevUI Mode (`"DevUI": true`)
- Interactive web chat interface
- Opens browser automatically
- OpenTelemetry tracing enabled

### API Mode (`"DevUI": false`)
- Exposes OpenAI-compatible REST endpoints ([spec](https://docs.ollama.com/api/openai-compatibility))
- Use agents as drop-in replacement for Ollama/OpenAI

## Running the Application

```bash
dotnet run                    # Starts in configured mode
```

## Code Conventions

- **DI-based tool classes** with constructor injection
- **Nullable reference types** enabled (`#nullable enable`)
- **Async/await** throughout with `CancellationToken` support
- **XML documentation** for tool descriptions (not `[Description]` attributes)
- **Typed response objects** from tools (not formatted strings)
- Configuration uses nested record-style classes in `AppSettings`

## Resources

- [Microsoft Agent Framework](https://github.com/microsoft/agent-framework) — Foundation SDK
- [Microsoft Foundry](https://learn.microsoft.com/en-us/azure/ai-foundry/what-is-foundry) — Production deployment
- [Ollama OpenAI Compatibility](https://docs.ollama.com/api/openai-compatibility) — API specification
