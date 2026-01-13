namespace Fetch.Models;

/// <summary>
/// Result of a web crawl operation.
/// </summary>
public sealed record CrawlResult
{
    /// <summary>
    /// The URL that was requested.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Whether the crawl was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// The HTML content if successful.
    /// </summary>
    public string? Html { get; init; }

    /// <summary>
    /// HTTP status code if available.
    /// </summary>
    public int? StatusCode { get; init; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Screenshot is ONLY populated on errors.
    /// </summary>
    public ScreenshotInfo? Screenshot { get; init; }

    /// <summary>
    /// Success - no screenshot.
    /// </summary>
    public static CrawlResult Success(string url, string html) =>
        new() { Url = url, IsSuccess = true, Html = html, Screenshot = null };

    /// <summary>
    /// HTTP error - with screenshot.
    /// </summary>
    public static CrawlResult Failed(string url, int statusCode, ScreenshotInfo? screenshot) =>
        new() { Url = url, IsSuccess = false, StatusCode = statusCode, ErrorMessage = $"HTTP {statusCode}", Screenshot = screenshot };

    /// <summary>
    /// Exception - with screenshot.
    /// </summary>
    public static CrawlResult Failed(string url, Exception ex, ScreenshotInfo? screenshot) =>
        new() { Url = url, IsSuccess = false, ErrorMessage = ex.Message, Screenshot = screenshot };

    /// <summary>
    /// Custom error - with screenshot.
    /// </summary>
    public static CrawlResult Failed(string url, string errorMessage, ScreenshotInfo? screenshot) =>
        new() { Url = url, IsSuccess = false, ErrorMessage = errorMessage, Screenshot = screenshot };
}
