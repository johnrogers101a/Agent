namespace Fetch;

/// <summary>
/// Configuration key constants for the Fetch tool.
/// </summary>
public static class ConfigKeys
{
    public const string ScreenshotDir = "Clients:Fetch:ScreenshotDir";
    public const string MaxScreenshots = "Clients:Fetch:MaxScreenshots";
    public const string TimeoutMs = "Clients:Fetch:TimeoutMs";
    public const string SearchApiKey = "Clients:Fetch:SearchApiKey";
}

/// <summary>
/// Default values for Fetch configuration.
/// </summary>
public static class Defaults
{
    public const int MaxScreenshots = 100;
    public const int TimeoutMs = 30_000;
    public const string ScreenshotDir = "fetch-screenshots";
}

/// <summary>
/// URL constants.
/// </summary>
public static class Urls
{
    public const string DuckDuckGoHtml = "https://html.duckduckgo.com/html/";
}

/// <summary>
/// Error message constants.
/// </summary>
public static class Errors
{
    public const string NavigationFailed = "Navigation failed - null response";
    public const string EmptyContent = "Empty content received";
    public const string TimeoutExpired = "Page load timed out after {0}ms";
}
