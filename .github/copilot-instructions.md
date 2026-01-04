# AI Coding Agent Instructions

## Architecture Overview

This is a .NET 10 AI Agent framework using **Microsoft.Agents.AI** SDK. The architecture follows a layered pattern:

```
Program.cs → AgentFactory → DevUIAwareAgent → AIAgent (Microsoft.Agents.AI)
                ↓
         ToolRegistry → Tools (WeatherTool, GmailTool)
                ↓
         Clients (external API integrations)
```

**Key architectural decisions:**
- Agents are configured declaratively in `appsettings.json`, not code
- Agent instructions live in markdown files under `Agents/` (e.g., `Agents/personal.md`)
- Tools are registered by name in `Tools/ToolRegistry.cs` and assigned to agents via config
- Two runtime modes: Console (direct prompt) or DevUI (web-based interactive chat)

## Adding New Capabilities

### Adding a New Tool
1. Create static tool class in `Tools/` following existing patterns:
   - Use `[Description]` attributes for function and parameters
   - Return `Task<string>` with human-readable results
   - Create `AIFunction` via `AIFunctionFactory.Create()`
2. Register in `Tools/ToolRegistry.cs` dictionary with a unique key
3. Add tool name to agent's `Tools` array in `appsettings.json`

Example from [WeatherTool.cs](Tools/WeatherTool.cs):
```csharp
[Description("Gets the current weather for a US zip code")]
public static async Task<string> GetWeatherByZip(
    [Description("The US zip code")] string zipCode) { ... }
```

### Adding External API Clients
1. Create folder under `Clients/` with service interface + implementation
2. Add configuration class to `Configuration/AppSettings.cs` under `ClientsSettings`
3. Instantiate in tool class using `AppSettings.LoadConfiguration()`

### Adding a New LLM Provider
1. Implement `IChatClientProvider` in `Infrastructure/Providers/`
2. Register in `ChatClientProviderFactory._providers` dictionary
3. Set `Provider.Type` in `appsettings.json`

Current providers: `Ollama` (local), `AzureFoundry` (Azure OpenAI with CLI or API key auth)

## Configuration

All runtime config is in `appsettings.json`:
- `Provider`: LLM settings (Type, Endpoint, ModelName, DevUI toggle)
- `Clients`: External API credentials (GoogleWeather, Gmail)
- `Agents`: Array of agent definitions with name, instructions file, and tool list

## Running the Application

```bash
dotnet run                    # Console mode (uses first agent)
```

Set `Provider.DevUI: true` for interactive web UI on configured port.

## Code Conventions

- **Static tool classes** with static `HttpClient` and service instances
- **Nullable reference types** enabled (`#nullable enable`)
- **Async/await** throughout with `CancellationToken` support
- Tool methods return formatted strings (not JSON) for LLM consumption
- Configuration uses nested record-style classes in `AppSettings`
