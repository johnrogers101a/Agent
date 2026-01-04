# Plan: Split Agent Project into Separate Libraries

The existing `src/AgentFramework/` (currently Agent.csproj) becomes a reusable library. We extract tools into GoogleTools and agent-specific code into PersonalAgent. PersonalAgent will be a minimal API project exposing Ollama-compatible endpoints with exact format matching. Namespaces match project/folder names.

## Steps

### 1. Refactor src/AgentFramework/ into a class library
- Keep Core/, Infrastructure/, Configuration/, and Tools/ToolRegistry.cs (returns empty dictionary)
- Remove Program.cs, Agents/, appsettings.json, WeatherTool.cs, GmailTool.cs, and all Clients/ folders
- Rename Agent.csproj → AgentFramework.csproj, change `<OutputType>` to `Library`
- Update namespaces to `AgentFramework.*`

### 2. Create src/GoogleTools/ class library
- Move WeatherTool.cs, GmailTool.cs, and all Clients/ subfolders here from AgentFramework
- Update namespaces to `GoogleTools.*`
- Add `<ProjectReference>` to AgentFramework.csproj

### 3. Create src/PersonalAgent/ minimal API project
- Move Program.cs, Agents/personal.md, and appsettings.json here
- Convert to minimal API with exact Ollama API format:
  - `POST /api/generate` — single prompt, returns `{model, created_at, response, done, done_reason, total_duration, load_duration, prompt_eval_count, prompt_eval_duration, eval_count, eval_duration}`
  - `POST /api/chat` — messages array with `{role, content}`, returns `{model, created_at, message: {role, content, tool_calls}, done, done_reason, ...durations}`
  - `GET /api/tags` — returns `{models: [{name, modified_at, size, digest, details: {format, family, parameter_size, quantization_level}}]}`
  - `GET /api/ps` — returns `{models: [{model, size, digest, details, expires_at, size_vram}]}`
  - Streaming: newline-delimited JSON with `done: false` until final `done: true`
- Create `PersonalAgent.Tools.ToolRegistry` that registers tools from GoogleTools
- Update namespaces to `PersonalAgent.*`
- Add `<ProjectReference>` to both AgentFramework and GoogleTools

### 4. Update Agent.sln
- Replace Agent.csproj reference with AgentFramework.csproj
- Add GoogleTools.csproj and PersonalAgent.csproj

### 5. Update using statements
- Fix all `using` directives across all three projects to reference new namespaces
