#nullable enable

using System.Text.Json;

namespace AgentFramework.Http;

/// <summary>
/// Base class for HTTP API clients providing common functionality.
/// </summary>
public abstract class ClientBase
{
    protected HttpClient HttpClient { get; }
    protected string BaseUrl { get; }
    protected JsonSerializerOptions? JsonOptions { get; }

    protected ClientBase(HttpClient httpClient, string baseUrl, JsonSerializerOptions? jsonOptions = null)
    {
        HttpClient = httpClient;
        BaseUrl = baseUrl.TrimEnd('/');
        JsonOptions = jsonOptions;
    }

    /// <summary>
    /// Sends a request and returns a typed response.
    /// </summary>
    protected async Task<ResponseBase<TResponse>> SendAsync<TResponse>(
        RequestBase request,
        CancellationToken cancellationToken = default)
    {
        request.Endpoint = BuildFullUrl(request.Endpoint);
        using var httpRequest = request.ToHttpRequestMessage(JsonOptions);
        var httpResponse = await HttpClient.SendAsync(httpRequest, cancellationToken);
        return await ResponseBase<TResponse>.FromHttpResponseAsync(httpResponse, JsonOptions, cancellationToken);
    }

    /// <summary>
    /// Sends a request with a body and returns a typed response.
    /// </summary>
    protected async Task<ResponseBase<TResponse>> SendAsync<TRequest, TResponse>(
        RequestBase<TRequest> request,
        CancellationToken cancellationToken = default)
    {
        request.Endpoint = BuildFullUrl(request.Endpoint);
        using var httpRequest = request.ToHttpRequestMessage(JsonOptions);
        var httpResponse = await HttpClient.SendAsync(httpRequest, cancellationToken);
        return await ResponseBase<TResponse>.FromHttpResponseAsync(httpResponse, JsonOptions, cancellationToken);
    }

    /// <summary>
    /// Sends a request and returns a non-generic response.
    /// </summary>
    protected async Task<ResponseBase> SendAsync(
        RequestBase request,
        CancellationToken cancellationToken = default)
    {
        request.Endpoint = BuildFullUrl(request.Endpoint);
        using var httpRequest = request.ToHttpRequestMessage(JsonOptions);
        var httpResponse = await HttpClient.SendAsync(httpRequest, cancellationToken);
        return await ResponseBase.FromHttpResponseAsync(httpResponse, cancellationToken);
    }

    /// <summary>
    /// Creates a GET request.
    /// </summary>
    protected RequestBase Get(string endpoint) => new(HttpMethod.Get, endpoint);

    /// <summary>
    /// Creates a POST request with a body.
    /// </summary>
    protected RequestBase<T> Post<T>(string endpoint, T body) => new(HttpMethod.Post, endpoint) { Body = body };

    /// <summary>
    /// Creates a POST request without a body.
    /// </summary>
    protected RequestBase Post(string endpoint) => new(HttpMethod.Post, endpoint);

    /// <summary>
    /// Creates a PUT request with a body.
    /// </summary>
    protected RequestBase<T> Put<T>(string endpoint, T body) => new(HttpMethod.Put, endpoint) { Body = body };

    /// <summary>
    /// Creates a DELETE request.
    /// </summary>
    protected RequestBase Delete(string endpoint) => new(HttpMethod.Delete, endpoint);

    private string BuildFullUrl(string endpoint)
    {
        if (endpoint.StartsWith("http://") || endpoint.StartsWith("https://"))
            return endpoint;
        
        return $"{BaseUrl}/{endpoint.TrimStart('/')}";
    }
}
