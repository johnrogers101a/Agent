using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using AgentFramework.Configuration;
using AgentFramework.Core;
using PersonalAgent.Api.Models;

namespace PersonalAgent.Api;

public static class OllamaEndpoints
{
    private static Dictionary<string, DevUIAwareAgent>? _agents;
    private static AppSettings? _settings;
    private const string AgentModelName = "personal-agent";

    public static void MapOllamaEndpoints(this WebApplication app, Dictionary<string, DevUIAwareAgent> agents, AppSettings settings)
    {
        _agents = agents;
        _settings = settings;

        app.MapPost("/api/generate", HandleGenerate);
        app.MapPost("/api/chat", HandleChat);
        app.MapGet("/api/tags", HandleTags);
        app.MapGet("/api/ps", HandlePs);
    }

    private static async Task HandleGenerate(HttpContext context, [FromBody] GenerateRequest request)
    {
        var agent = GetAgent(request.Model);
        if (agent is null)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(new { error = $"Model '{request.Model}' not found" });
            return;
        }

        var startTime = Stopwatch.GetTimestamp();
        var createdAt = DateTime.UtcNow.ToString("o");

        if (request.Stream)
        {
            context.Response.ContentType = "application/x-ndjson";
            await StreamGenerateResponse(context, agent, request.Prompt, request.Model, createdAt, startTime);
        }
        else
        {
            var response = await RunAgentAsync(agent, request.Prompt);
            var elapsed = Stopwatch.GetElapsedTime(startTime);

            await context.Response.WriteAsJsonAsync(new GenerateResponse
            {
                Model = request.Model,
                CreatedAt = createdAt,
                Response = response,
                Done = true,
                DoneReason = "stop",
                TotalDuration = (long)elapsed.TotalNanoseconds,
                LoadDuration = 0,
                PromptEvalCount = request.Prompt.Length,
                PromptEvalDuration = (long)(elapsed.TotalNanoseconds * 0.1),
                EvalCount = response.Length,
                EvalDuration = (long)(elapsed.TotalNanoseconds * 0.9)
            });
        }
    }

    private static async Task StreamGenerateResponse(HttpContext context, DevUIAwareAgent agent, string prompt, string model, string createdAt, long startTime)
    {
        var response = await RunAgentAsync(agent, prompt);
        var elapsed = Stopwatch.GetElapsedTime(startTime);

        // Stream response word by word
        var words = response.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            var word = i < words.Length - 1 ? words[i] + " " : words[i];
            var chunk = new GenerateResponse
            {
                Model = model,
                CreatedAt = DateTime.UtcNow.ToString("o"),
                Response = word,
                Done = false
            };

            await WriteNdjsonLine(context, chunk);
            await Task.Delay(10); // Small delay for realistic streaming
        }

        // Final response with done=true
        var finalResponse = new GenerateResponse
        {
            Model = model,
            CreatedAt = createdAt,
            Response = "",
            Done = true,
            DoneReason = "stop",
            TotalDuration = (long)elapsed.TotalNanoseconds,
            LoadDuration = 0,
            PromptEvalCount = prompt.Length,
            PromptEvalDuration = (long)(elapsed.TotalNanoseconds * 0.1),
            EvalCount = response.Length,
            EvalDuration = (long)(elapsed.TotalNanoseconds * 0.9)
        };

        await WriteNdjsonLine(context, finalResponse);
    }

    private static async Task HandleChat(HttpContext context, [FromBody] ChatRequest request)
    {
        var agent = GetAgent(request.Model);
        if (agent is null)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(new { error = $"Model '{request.Model}' not found" });
            return;
        }

        var startTime = Stopwatch.GetTimestamp();
        var createdAt = DateTime.UtcNow.ToString("o");

        // Build prompt from messages
        var prompt = BuildPromptFromMessages(request.Messages);

        if (request.Stream)
        {
            context.Response.ContentType = "application/x-ndjson";
            await StreamChatResponse(context, agent, prompt, request.Model, createdAt, startTime);
        }
        else
        {
            var response = await RunAgentAsync(agent, prompt);
            var elapsed = Stopwatch.GetElapsedTime(startTime);

            await context.Response.WriteAsJsonAsync(new ChatResponse
            {
                Model = request.Model,
                CreatedAt = createdAt,
                Message = new ChatMessage
                {
                    Role = "assistant",
                    Content = response
                },
                Done = true,
                DoneReason = "stop",
                TotalDuration = (long)elapsed.TotalNanoseconds,
                LoadDuration = 0,
                PromptEvalCount = prompt.Length,
                PromptEvalDuration = (long)(elapsed.TotalNanoseconds * 0.1),
                EvalCount = response.Length,
                EvalDuration = (long)(elapsed.TotalNanoseconds * 0.9)
            });
        }
    }

    private static async Task StreamChatResponse(HttpContext context, DevUIAwareAgent agent, string prompt, string model, string createdAt, long startTime)
    {
        var response = await RunAgentAsync(agent, prompt);
        var elapsed = Stopwatch.GetElapsedTime(startTime);

        // Stream response word by word
        var words = response.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            var word = i < words.Length - 1 ? words[i] + " " : words[i];
            var chunk = new ChatResponse
            {
                Model = model,
                CreatedAt = DateTime.UtcNow.ToString("o"),
                Message = new ChatMessage
                {
                    Role = "assistant",
                    Content = word
                },
                Done = false
            };

            await WriteNdjsonLine(context, chunk);
            await Task.Delay(10); // Small delay for realistic streaming
        }

        // Final response with done=true
        var finalResponse = new ChatResponse
        {
            Model = model,
            CreatedAt = createdAt,
            Message = new ChatMessage
            {
                Role = "assistant",
                Content = ""
            },
            Done = true,
            DoneReason = "stop",
            TotalDuration = (long)elapsed.TotalNanoseconds,
            LoadDuration = 0,
            PromptEvalCount = prompt.Length,
            PromptEvalDuration = (long)(elapsed.TotalNanoseconds * 0.1),
            EvalCount = response.Length,
            EvalDuration = (long)(elapsed.TotalNanoseconds * 0.9)
        };

        await WriteNdjsonLine(context, finalResponse);
    }

    private static Task<IResult> HandleTags()
    {
        var models = _agents?.Keys.Select(name => new ModelInfo
        {
            Name = name.ToLowerInvariant(),
            ModifiedAt = DateTime.UtcNow.ToString("o"),
            Size = 0,
            Digest = GenerateDigest(name),
            Details = new ModelDetails
            {
                Format = "gguf",
                Family = "agent",
                ParameterSize = "N/A",
                QuantizationLevel = "N/A"
            }
        }).ToList() ?? [];

        return Task.FromResult(Results.Ok(new TagsResponse { Models = models }));
    }

    private static Task<IResult> HandlePs()
    {
        var models = _agents?.Keys.Select(name => new RunningModel
        {
            Model = name.ToLowerInvariant(),
            Size = 0,
            Digest = GenerateDigest(name),
            Details = new ModelDetails
            {
                Format = "gguf",
                Family = "agent",
                ParameterSize = "N/A",
                QuantizationLevel = "N/A"
            },
            ExpiresAt = DateTime.UtcNow.AddHours(1).ToString("o"),
            SizeVram = 0
        }).ToList() ?? [];

        return Task.FromResult(Results.Ok(new PsResponse { Models = models }));
    }

    private static DevUIAwareAgent? GetAgent(string model)
    {
        if (_agents is null) return null;

        // Try exact match first
        if (_agents.TryGetValue(model, out var agent))
            return agent;

        // Try case-insensitive match
        var match = _agents.Keys.FirstOrDefault(k => 
            k.Equals(model, StringComparison.OrdinalIgnoreCase));
        
        if (match is not null)
            return _agents[match];

        // Return first agent as default
        return _agents.Values.FirstOrDefault();
    }

    private static string BuildPromptFromMessages(List<ChatMessage> messages)
    {
        var sb = new StringBuilder();
        foreach (var msg in messages)
        {
            if (msg.Role == "user")
            {
                sb.AppendLine($"User: {msg.Content}");
            }
            else if (msg.Role == "assistant")
            {
                sb.AppendLine($"Assistant: {msg.Content}");
            }
            else if (msg.Role == "system")
            {
                sb.AppendLine($"System: {msg.Content}");
            }
        }
        return sb.ToString().Trim();
    }

    private static async Task<string> RunAgentAsync(DevUIAwareAgent agent, string prompt)
    {
        // For now, use a simple approach - capture console output
        // In a real implementation, you'd want to modify DevUIAwareAgent to return the response
        try
        {
            var tcs = new TaskCompletionSource<string>();
            var originalOut = Console.Out;
            var sw = new StringWriter();
            Console.SetOut(sw);

            try
            {
                await agent.RunAsync(prompt);
                var output = sw.ToString();
                
                // Extract the actual response from log output
                var responsePrefix = "response:";
                var responseIndex = output.IndexOf(responsePrefix, StringComparison.OrdinalIgnoreCase);
                if (responseIndex >= 0)
                {
                    return output[(responseIndex + responsePrefix.Length)..].Trim();
                }
                
                return output.Trim();
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
        catch (Exception ex)
        {
            return $"Error processing request: {ex.Message}";
        }
    }

    private static async Task WriteNdjsonLine<T>(HttpContext context, T obj)
    {
        var json = JsonSerializer.Serialize(obj);
        await context.Response.WriteAsync(json + "\n");
        await context.Response.Body.FlushAsync();
    }

    private static string GenerateDigest(string name)
    {
        var bytes = Encoding.UTF8.GetBytes(name);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()[..12]}";
    }
}
