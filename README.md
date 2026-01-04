# Agent

A .NET 10 framework for quickly spinning up AI agents with tool support using Microsoft.Agents.AI.

## Installation

```bash
dotnet add package Agent
```

## Quick Start

### 1. Create `appsettings.json`

```json
{
  "Provider": {
    "Type": "Ollama",
    "Endpoint": "http://localhost:11434",
    "ModelName": "llama3.2:latest",
    "DevUI": false,
    "DevUIPort": 8080
  },
  "Agents": [
    {
      "Name": "MyAgent",
      "InstructionsFileName": "Agents/assistant.md",
      "Tools": []
    }
  ]
}
```

### 2. Create agent instructions

Create `Agents/assistant.md`:
```markdown
You are a helpful assistant.
```

### 3. Run your agent

```csharp
using Agent.Configuration;
using Agent.Core;

var appSettings = AppSettings.LoadConfiguration();
var agents = AgentFactory.Load(appSettings);

await agents["MyAgent"].RunAsync("Hello, what can you do?");
```

## Providers

| Provider | Type | Auth |
|----------|------|------|
| Ollama | `Ollama` | None (local) |
| Azure OpenAI | `AzureFoundry` | Azure CLI or API key |

## Adding Tools

See [.github/copilot-instructions.md](.github/copilot-instructions.md) for patterns on extending agents with custom tools.

## DevUI Mode

Set `"DevUI": true` in config to launch an interactive web chat interface instead of console mode.