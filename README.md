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
├── src/AgentFramework/        # Core framework library
├── src/Crawl4AI.Net/          # LLM-optimized content extraction library
├── src/Agents/Personal/       # Example agent application
└── src/Tools/                 # Tool libraries
    ├── Weather/               # Google Weather API tools
    ├── Gmail/                 # Gmail API tools
    ├── Graph/                 # Microsoft Graph API tools
    ├── Hotmail/               # Hotmail/Outlook consumer tools
    ├── Fetch/                 # Web search and content extraction tools
    └── Common/                # Shared tool utilities
```

## Developer Workflows

```powershell
# Run the agent locally
.\src\scripts\start-agent.ps1

# Deploy to Azure AI Foundry (idempotent, uses Pester tests for validation)
.\src\scripts\deploy.ps1
```

### Azure Deployment Details

The `deploy.ps1` script creates all required infrastructure:
- Resource Group, AI Services Account, Foundry Project
- Model Deployment (gpt-4.1 via GlobalStandard SKU)
- Network Rules (IP whitelisting), RBAC Role Assignment
- Storage Account + Azure Functions (Flex Consumption)

Configuration in `src/scripts/AzFoundryDeploy/Config.psd1`.

## Providers

| Provider | Type | Auth | Use Case |
|----------|------|------|----------|
| Ollama | `Ollama` | None | Local development |
| Azure OpenAI | `AzureFoundry` | Azure CLI or API key | Production deployment |

## Email Tools

| Provider | Tools | Auth |
|----------|-------|------|
| Gmail | `GetGmail`, `SearchGmail`, `GetGmailContents` | Google OAuth2 + PKCE |
| Graph | `GraphApiTool` | Azure.Identity (Work/School accounts) |
| Hotmail | `GetHotmail`, `SearchHotmail`, `GetHotmailContents` | Azure.Identity (Personal accounts) |

## Web Fetch Tools

| Tool | Description |
|------|-------------|
| `WebSearch` | Searches the web using DuckDuckGo (no API key required) |
| `GetPageContent` | Fetches a web page and extracts LLM-optimized markdown |

### Crawl4AI.Net Library

A standalone C# port of [Crawl4AI](https://github.com/unclecode/crawl4ai) content extraction algorithms:

- **PruningContentFilter** — Removes boilerplate using text density, link density, and tag importance scoring
- **Bm25ContentFilter** — Query-based content filtering using BM25 relevance ranking
- **MarkdownGenerator** — Converts HTML to clean markdown with `fit_markdown` output

### WebCrawler Features

- Headless Playwright browser (Azure-compatible)
- **Error-only screenshots** for debugging failed page loads
- Extensive `ILogger<T>` integration
- Console message and request failure tracking

## Runtime Modes

| Mode | Config | Description |
|------|--------|-------------|
| DevUI | `"DevUI": true` | Interactive web chat interface with OpenTelemetry tracing |
| API | `"DevUI": false` | OpenAI-compatible REST API ([spec](https://docs.ollama.com/api/openai-compatibility)) |

## Resources

- [Microsoft Agent Framework](https://github.com/microsoft/agent-framework) — The foundation this framework builds on
- [Microsoft Foundry](https://learn.microsoft.com/en-us/azure/ai-foundry/what-is-foundry) — Production deployment platform
- [Ollama OpenAI Compatibility](https://docs.ollama.com/api/openai-compatibility) — API specification
- [Crawl4AI](https://github.com/unclecode/crawl4ai) — Original Python library for LLM-optimized content extraction
- [Copilot Instructions](.github/copilot-instructions.md) — Detailed patterns for extending agents