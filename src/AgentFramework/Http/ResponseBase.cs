#nullable enable

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AgentFramework.Http;

/// <summary>
/// Base class for HTTP responses with standard operation details.
/// </summary>
/// <typeparam name="T">The type of the response data.</typeparam>
public class ResponseBase<T>
{
    public HttpStatusCode StatusCode { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public T? Data { get; set; }
    public string? Error { get; set; }
    public string? RawContent { get; set; }

    /// <summary>
    /// True if the status code is in the 2xx range.
    /// </summary>
    public bool IsSuccess => (int)StatusCode >= 200 && (int)StatusCode < 300;

    /// <summary>
    /// True if status code is 201 Created.
    /// </summary>
    public bool IsCreated => StatusCode == HttpStatusCode.Created;

    /// <summary>
    /// True if status code is 202 Accepted.
    /// </summary>
    public bool IsAccepted => StatusCode == HttpStatusCode.Accepted;

    /// <summary>
    /// True if status code is 204 No Content.
    /// </summary>
    public bool IsNoContent => StatusCode == HttpStatusCode.NoContent;

    /// <summary>
    /// True if status code is 404 Not Found.
    /// </summary>
    public bool IsNotFound => StatusCode == HttpStatusCode.NotFound;

    /// <summary>
    /// True if status code is 401 Unauthorized.
    /// </summary>
    public bool IsUnauthorized => StatusCode == HttpStatusCode.Unauthorized;

    /// <summary>
    /// Creates a ResponseBase from an HttpResponseMessage.
    /// </summary>
    public static async Task<ResponseBase<T>> FromHttpResponseAsync(
        HttpResponseMessage response,
        JsonSerializerOptions? jsonOptions = null,
        CancellationToken cancellationToken = default)
    {
        var result = new ResponseBase<T>
        {
            StatusCode = response.StatusCode
        };

        // Copy headers
        foreach (var header in response.Headers)
        {
            result.Headers[header.Key] = string.Join(", ", header.Value);
        }

        // Read content
        if (response.Content.Headers.ContentLength > 0 || response.Content.Headers.ContentType is not null)
        {
            result.RawContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    result.Data = await response.Content.ReadFromJsonAsync<T>(jsonOptions, cancellationToken);
                }
                catch (JsonException)
                {
                    // If JSON parsing fails, Data stays null, RawContent has the content
                }
            }
            else
            {
                result.Error = result.RawContent;
            }
        }

        return result;
    }

    /// <summary>
    /// Creates a successful response with data.
    /// </summary>
    public static ResponseBase<T> Success(T data, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new ResponseBase<T>
        {
            StatusCode = statusCode,
            Data = data
        };
    }

    /// <summary>
    /// Creates a failed response with an error message.
    /// </summary>
    public static ResponseBase<T> Failure(string error, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
    {
        return new ResponseBase<T>
        {
            StatusCode = statusCode,
            Error = error
        };
    }
}

/// <summary>
/// Non-generic response base for responses without typed data.
/// </summary>
public class ResponseBase : ResponseBase<object>
{
    public static async Task<ResponseBase> FromHttpResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        var result = new ResponseBase
        {
            StatusCode = response.StatusCode
        };

        foreach (var header in response.Headers)
        {
            result.Headers[header.Key] = string.Join(", ", header.Value);
        }

        if (response.Content.Headers.ContentLength > 0 || response.Content.Headers.ContentType is not null)
        {
            result.RawContent = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                result.Error = result.RawContent;
            }
        }

        return result;
    }
}
