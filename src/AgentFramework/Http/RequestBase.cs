#nullable enable

using System.Net.Http.Headers;
using System.Text.Json;

namespace AgentFramework.Http;

/// <summary>
/// Base class for HTTP requests with standard operation details.
/// </summary>
/// <typeparam name="T">The type of the request body.</typeparam>
public class RequestBase<T>
{
    public HttpMethod Method { get; set; } = HttpMethod.Get;
    public string Endpoint { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; } = new();
    public Dictionary<string, string> QueryParams { get; } = new();
    public T? Body { get; set; }

    public RequestBase() { }

    public RequestBase(string endpoint) => Endpoint = endpoint;

    public RequestBase(HttpMethod method, string endpoint)
    {
        Method = method;
        Endpoint = endpoint;
    }

    /// <summary>
    /// Sets Bearer token authentication header.
    /// </summary>
    public RequestBase<T> SetBearerAuth(string token)
    {
        Headers["Authorization"] = $"Bearer {token}";
        return this;
    }

    /// <summary>
    /// Adds a query parameter.
    /// </summary>
    public RequestBase<T> AddQueryParam(string key, string value)
    {
        QueryParams[key] = value;
        return this;
    }

    /// <summary>
    /// Adds a query parameter if the value is not null or empty.
    /// </summary>
    public RequestBase<T> AddQueryParamIfNotEmpty(string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
            QueryParams[key] = value;
        return this;
    }

    /// <summary>
    /// Builds the full URL with query parameters.
    /// </summary>
    public string BuildUrl()
    {
        if (QueryParams.Count == 0)
            return Endpoint;

        var queryString = string.Join("&", QueryParams.Select(kv => 
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
        
        return $"{Endpoint}?{queryString}";
    }

    /// <summary>
    /// Converts this request to an HttpRequestMessage.
    /// </summary>
    public HttpRequestMessage ToHttpRequestMessage(JsonSerializerOptions? jsonOptions = null)
    {
        var request = new HttpRequestMessage(Method, BuildUrl());

        foreach (var header in Headers)
        {
            if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
            {
                var parts = header.Value.Split(' ', 2);
                if (parts.Length == 2)
                    request.Headers.Authorization = new AuthenticationHeaderValue(parts[0], parts[1]);
            }
            else
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        if (Body is not null && (Method == HttpMethod.Post || Method == HttpMethod.Put || Method == HttpMethod.Patch))
        {
            request.Content = JsonContent.Create(Body, options: jsonOptions);
        }

        return request;
    }
}

/// <summary>
/// Non-generic request base for requests without a body.
/// </summary>
public class RequestBase : RequestBase<object>
{
    public RequestBase() : base() { }
    public RequestBase(string endpoint) : base(endpoint) { }
    public RequestBase(HttpMethod method, string endpoint) : base(method, endpoint) { }

    /// <summary>
    /// Sets Bearer token authentication header.
    /// </summary>
    public new RequestBase SetBearerAuth(string token)
    {
        Headers["Authorization"] = $"Bearer {token}";
        return this;
    }

    /// <summary>
    /// Adds a query parameter.
    /// </summary>
    public new RequestBase AddQueryParam(string key, string value)
    {
        QueryParams[key] = value;
        return this;
    }

    /// <summary>
    /// Adds a query parameter if the value is not null or empty.
    /// </summary>
    public new RequestBase AddQueryParamIfNotEmpty(string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
            QueryParams[key] = value;
        return this;
    }
}
