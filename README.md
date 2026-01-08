# Agent Framework

A .NET 10 framework for quickly spinning up AI agents with tool support. Built on [Microsoft Agent Framework](https://github.com/microsoft/agent-framework) and designed for deployment to [Microsoft Foundry](https://learn.microsoft.com/en-us/azure/ai-foundry/what-is-foundry).

## Why This Framework?

- **4 lines of code** to create a fully functional agent with tools
- **Automatic tool discovery** via `[McpTool]` attribute and XML documentation
- **OpenAI-compatible API** for integration with existing tooling ([Ollama API spec](https://docs.ollama.com/api/openai-compatibility))
- **Multi-project architecture** for organizing tools into reusable libraries

## Quick Start

### 1. Program.cs (4 lines)

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.ConfigureAgent();

var app = builder.Build();
app.UseAgents();
app.Run();
```

### 2. appsettings.json

```json
{
  "Provider": {
    "Type": "Ollama",
    "Endpoint": "http://localhost:11434",
    "ModelName": "llama3.2:latest",
    "DevUI": true
  },
  "Agents": [
    {
      "Name": "MyAgent",
      "InstructionsFileName": "Agents/assistant.md",
      "Tools": ["GetWeatherByZip"]
    }
  ]
}
```

### 3. Create a Tool

```csharp
/// <summary>
/// Gets current weather for a US zip code.
/// </summary>
public class GetWeatherByZip
{
    private readonly HttpClient _http;

    public GetWeatherByZip(HttpClient httpClient) => _http = httpClient;

    /// <summary>
    /// Gets current weather for a US zip code.
    /// </summary>
    /// <param name="zipCode">US zip code (e.g., 98052).</param>
    [McpTool]
    public async Task<WeatherResponse> ExecuteAsync(string zipCode)
    {
        // Your implementation here
    }
}
```

Tools are automatically discovered from referenced assemblies. No registration required.

## Project Structure

```
Agent.sln
├── src/AgentFramework/     # Core framework library
├── src/PersonalAgent/      # Your agent application
├── src/Gmail/              # Gmail tools library
└── src/Weather/            # Weather tools library
```

## Providers

| Provider | Type | Auth | Use Case |
|----------|------|------|----------|
| Ollama | `Ollama` | None | Local development |
| Azure OpenAI | `AzureFoundry` | Azure CLI or API key | Production deployment |

## Runtime Modes

| Mode | Config | Description |
|------|--------|-------------|
| DevUI | `"DevUI": true` | Interactive web chat interface |
| API | `"DevUI": false` | OpenAI-compatible REST API ([spec](https://docs.ollama.com/api/openai-compatibility)) |

## Resources

- [Microsoft Agent Framework](https://github.com/microsoft/agent-framework) — The foundation this framework builds on
- [Microsoft Foundry](https://learn.microsoft.com/en-us/azure/ai-foundry/what-is-foundry) — Production deployment platform
- [Ollama OpenAI Compatibility](https://docs.ollama.com/api/openai-compatibility) — API specification
- [Copilot Instructions](.github/copilot-instructions.md) — Detailed patterns for extending agents