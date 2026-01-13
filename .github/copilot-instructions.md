# AI Coding Agent Instructions

## Architecture Overview

This is a .NET 10 AI Agent framework built on [Microsoft Agent Framework](https://github.com/microsoft/agent-framework) (`Microsoft.Agents.AI` SDK), designed for deployment to [Microsoft Foundry](https://learn.microsoft.com/en-us/azure/ai-foundry/what-is-foundry).

### Project Structure

```
Agent.sln
├── src/AgentFramework/        # Core framework library
│   ├── Core/                  # AgentFactory, McpToolDiscovery, ConversationMemory
│   ├── Attributes/            # McpToolAttribute
│   ├── Extensions/            # ConfigureAgent(), UseAgents()
│   └── Infrastructure/        # ChatClientProviderFactory, Providers/
├── src/Crawl4AI.Net/          # LLM-optimized content extraction library
│   ├── Abstractions/          # IContentFilter, IMarkdownGenerator, MarkdownResult
│   ├── Filters/               # PruningContentFilter, Bm25ContentFilter
│   ├── Markdown/              # MarkdownGenerator (HTML-to-Markdown)
│   ├── Html/                  # HtmlCleaner utilities
│   └── Algorithms/            # Bm25Okapi, EnglishStemmer
├── src/Agents/Personal/       # Example agent application (4 lines of code)
├── src/Tools/                 # Tool libraries (auto-discovered via [McpTool])
│   ├── Weather/               # Google Weather API tools
│   ├── Gmail/                 # Gmail API tools
│   ├── Graph/                 # Microsoft Graph API tools
│   ├── Hotmail/               # Hotmail/Outlook consumer tools
│   ├── Fetch/                 # Web search and content extraction tools
│   └── Common/                # Shared tool utilities
└── src/scripts/               # Deployment and startup scripts
    └── AzFoundryDeploy/       # PowerShell module for Azure deployment
```

### Key Architectural Decisions

- **4-line Program.cs** using `ConfigureAgent()` and `UseAgents()` extension methods
- **Declarative agent config** in `appsettings.json`, not code
- **Agent instructions** in markdown files under `Agents/` folder
- **Automatic tool discovery** via `[McpTool]` attribute — no manual registration
- **XML documentation** provides tool and parameter descriptions (not `[Description]` attributes)
- **DI-based tools** with constructor injection for `HttpClient`, `IConfiguration`, `ILogger`, etc.

## Adding a New Tool

1. Create a tool class in `src/Tools/{ServiceName}/Tools/`:
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
        _apiKey = config[ConfigKeys.ApiKey]!;  // Use constants, not magic strings
    }

    /// <summary>Gets current weather for a US zip code.</summary>
    /// <param name="zipCode">US zip code (e.g., 98052).</param>
    [McpTool]
    public async Task<CurrentWeatherResponse> ExecuteAsync(string zipCode)
    {
        // Return typed response objects, not formatted strings
    }
}
```

2. Add the tool name to the agent's `Tools` array in `appsettings.json`

## Configuration

All runtime config is in `appsettings.json`:
- `Provider.Type`: `Ollama` (local) or `AzureFoundry` (Azure OpenAI)
- `Provider.DevUI`: `true` for interactive chat, `false` for REST API mode
- `Clients.*`: API keys and OAuth config for external services
- `Agents[].Tools`: Array of tool class names to enable

## Email Tool Providers

Three separate email integrations with different auth mechanisms:

| Provider | Tools | Auth | Tenant |
|----------|-------|------|--------|
| **Gmail** | `GetGmail`, `SearchGmail`, `GetGmailContents` | Google OAuth2 + PKCE via `AuthService` | N/A |
| **Graph** | `GraphApiTool` (generic Graph API) | Azure.Identity (`InteractiveBrowserCredential` or `DeviceCodeCredential`) | Work/School (Azure AD) |
| **Hotmail** | `GetHotmail`, `SearchHotmail`, `GetHotmailContents` | Azure.Identity with `TenantId: "consumers"` | Personal Microsoft accounts |

- Gmail uses custom `AuthService` with local OAuth callback and PKCE
- Graph/Hotmail use `*ClientFactory` classes with Azure.Identity token cache persistence
- Graph supports any Azure AD tenant; Hotmail hardcodes `"consumers"` tenant

## Web Fetch Tools

The Fetch tool provides web search and content extraction:

| Tool | Description |
|------|-------------|
| **WebSearch** | Searches the web using DuckDuckGo (no API key required) |
| **GetPageContent** | Fetches a web page and extracts LLM-optimized markdown |

### Crawl4AI.Net Library

Standalone C# port of [Crawl4AI](https://github.com/unclecode/crawl4ai) content extraction algorithms:

- **PruningContentFilter**: Removes boilerplate using text density, link density, and tag importance scoring
- **Bm25ContentFilter**: Query-based content filtering using BM25 relevance ranking
- **MarkdownGenerator**: Converts HTML to clean markdown with `fit_markdown` output

### WebCrawler Service

Headless Playwright browser with:
- Azure-compatible headless mode (no display required)
- **Error-only screenshots** - captures diagnostic screenshots only on failures
- Extensive logging via `ILogger<T>` for debugging
- Console message and request failure tracking

Configuration in `appsettings.json`:
```json
"Fetch": {
  "ScreenshotDir": "./screenshots",
  "MaxScreenshots": 100,
  "TimeoutMs": 30000
}
```

## Scripts and Deployment

### `src/scripts/start-agent.ps1`
Simple wrapper that runs `dotnet run` in the Personal agent project.

### `src/scripts/deploy.ps1`
Idempotent Azure deployment script that orchestrates full infrastructure:
1. Resource Group, AI Services Account, Foundry Project
2. Model Deployment (gpt-4.1 via GlobalStandard SKU)
3. Network Rules (IP whitelisting), RBAC Role Assignment
4. Storage Account + Azure Functions (Flex Consumption) for durable endpoints

Uses Pester tests for validation. Requires Azure CLI login.

### `src/scripts/AzFoundryDeploy/` module structure:
- `Config.psd1` — All deployment settings (subscription, resource names, model config)
- `Private/` — Individual deployment functions:
  - `Initialize-*.ps1` — ResourceGroup, AIServiceAccount, FoundryProject, ModelDeployment, StorageAccount, FunctionApp
  - `Add-NetworkRule.ps1`, `Add-RbacRoleAssignment.ps1`
  - `Update-AppSettings.ps1` — Patches appsettings.json with deployed endpoints
- `Tests/` — Pester tests (`Deploy-AzFoundry.Tests.ps1`, `Deploy-DurableAgent.Tests.ps1`)

## Code Conventions

- **Nullable reference types** enabled (`#nullable enable`)
- **Constants classes** for config keys, URLs, error messages (see `Constants.cs` in each tool project)
- **Typed response objects** from tools (not formatted strings)
- **Async/await** throughout with proper logging
- Configuration uses `ConfigKeys` constants, not magic strings
- [Microsoft Foundry](https://learn.microsoft.com/en-us/azure/ai-foundry/what-is-foundry) — Production deployment
- [Ollama OpenAI Compatibility](https://docs.ollama.com/api/openai-compatibility) — API specification
