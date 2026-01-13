namespace Fetch.Models;

/// <summary>
/// Screenshot metadata for debugging errors.
/// </summary>
public sealed record ScreenshotInfo
{
    /// <summary>
    /// Path to the screenshot file.
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// The URL being captured.
    /// </summary>
    public string Url { get; init; } = "";

    /// <summary>
    /// Label describing the error type.
    /// </summary>
    public string Label { get; init; } = "";

    /// <summary>
    /// When the screenshot was captured.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Page title at time of capture.
    /// </summary>
    public string? PageTitle { get; init; }

    /// <summary>
    /// Actual URL (may differ from requested URL after redirects).
    /// </summary>
    public string? CurrentUrl { get; init; }

    /// <summary>
    /// Browser console messages captured.
    /// </summary>
    public List<string> ConsoleMessages { get; init; } = new();

    /// <summary>
    /// Error message if screenshot capture failed.
    /// </summary>
    public string? Error { get; init; }
}
