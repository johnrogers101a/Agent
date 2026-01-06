using System.Net.Http.Json;
using System.Text.Json;

namespace AgentFramework.Api;

/// <summary>
/// Provides utilities for querying Ollama model information.
/// </summary>
public static class OllamaModelInfo
{
    private const int DefaultContextLength = 4096;
    private static readonly Dictionary<string, int> _contextLengthCache = new();

    /// <summary>
    /// Gets the context length for a model from Ollama's /api/show endpoint.
    /// </summary>
    /// <param name="httpClient">The HTTP client configured for Ollama.</param>
    /// <param name="modelName">The name of the model to query.</param>
    /// <returns>The context length in tokens, or 4096 as fallback.</returns>
    public static async Task<int> GetContextLengthAsync(HttpClient httpClient, string modelName)
    {
        // Check cache first
        if (_contextLengthCache.TryGetValue(modelName, out var cachedLength))
        {
            return cachedLength;
        }

        try
        {
            var response = await httpClient.PostAsJsonAsync("/api/show", new { model = modelName });
            
            if (!response.IsSuccessStatusCode)
            {
                return CacheAndReturn(modelName, DefaultContextLength);
            }

            var content = await response.Content.ReadAsStringAsync();
            var contextLength = ParseContextLength(content);
            
            return CacheAndReturn(modelName, contextLength ?? DefaultContextLength);
        }
        catch
        {
            // On any error, return default
            return CacheAndReturn(modelName, DefaultContextLength);
        }
    }

    /// <summary>
    /// Gets the context length synchronously (uses cached value or default).
    /// </summary>
    /// <param name="modelName">The model name to look up.</param>
    /// <returns>The cached context length or default.</returns>
    public static int GetCachedContextLength(string modelName)
    {
        return _contextLengthCache.TryGetValue(modelName, out var length) ? length : DefaultContextLength;
    }

    /// <summary>
    /// Clears the context length cache.
    /// </summary>
    public static void ClearCache()
    {
        _contextLengthCache.Clear();
    }

    /// <summary>
    /// Parses the context_length from Ollama's /api/show response.
    /// Looks for any key ending in ".context_length" in model_info.
    /// </summary>
    private static int? ParseContextLength(string jsonContent)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            // Look for model_info object
            if (!root.TryGetProperty("model_info", out var modelInfo))
            {
                return null;
            }

            // Scan for any property ending with ".context_length"
            // e.g., "llama.context_length", "qwen2.context_length", etc.
            foreach (var property in modelInfo.EnumerateObject())
            {
                if (property.Name.EndsWith(".context_length", StringComparison.OrdinalIgnoreCase))
                {
                    if (property.Value.TryGetInt32(out var contextLength))
                    {
                        return contextLength;
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static int CacheAndReturn(string modelName, int contextLength)
    {
        _contextLengthCache[modelName] = contextLength;
        return contextLength;
    }
}
