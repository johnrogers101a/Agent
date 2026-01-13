using System.Diagnostics;
using Fetch.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace Fetch.Services;

/// <summary>
/// Headless Playwright browser for fetching web pages with diagnostic logging.
/// </summary>
public sealed class WebCrawler : IAsyncDisposable
{
    private readonly ILogger<WebCrawler> _logger;
    private readonly ScreenshotLogger _screenshotLogger;
    private readonly int _timeoutMs;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public WebCrawler(
        ILogger<WebCrawler> logger,
        ScreenshotLogger screenshotLogger,
        IConfiguration config)
    {
        _logger = logger;
        _screenshotLogger = screenshotLogger;
        _timeoutMs = config.GetValue(ConfigKeys.TimeoutMs, Defaults.TimeoutMs);

        _logger.LogDebug("WebCrawler initialized with timeout {TimeoutMs}ms", _timeoutMs);
    }

    /// <summary>
    /// Fetches the HTML content of a URL using headless Chromium.
    /// </summary>
    public async Task<CrawlResult> FetchAsync(string url, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting fetch for {Url}", url);

        var stopwatch = Stopwatch.StartNew();
        await EnsureBrowserAsync();
        _logger.LogDebug("Browser ready in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

        var page = await _browser!.NewPageAsync();
        _logger.LogDebug("New page created for {Url}", url);

        // Capture console messages for diagnostics
        var consoleMessages = new List<string>();
        page.Console += (_, msg) =>
        {
            var message = $"[{msg.Type}] {msg.Text}";
            consoleMessages.Add(message);
            
            if (msg.Type == "error")
                _logger.LogWarning("Browser console error on {Url}: {Message}", url, msg.Text);
            else
                _logger.LogDebug("Browser console [{Type}] on {Url}: {Message}", msg.Type, url, msg.Text);
        };

        // Capture request failures
        page.RequestFailed += (_, request) =>
        {
            _logger.LogWarning("Request failed on {Url}: {FailedUrl} - {Failure}",
                url, request.Url, request.Failure);
        };

        // Track response for logging
        page.Response += (_, response) =>
        {
            if (response.Url == url)
                _logger.LogDebug("Response received for {Url}: HTTP {Status}", url, response.Status);
        };

        try
        {
            _logger.LogDebug("Navigating to {Url} with timeout {TimeoutMs}ms", url, _timeoutMs);
            stopwatch.Restart();

            var response = await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = _timeoutMs
            });

            var navigationTime = stopwatch.ElapsedMilliseconds;
            _logger.LogDebug("Navigation completed in {ElapsedMs}ms for {Url}", navigationTime, url);

            // Check for null response (navigation failed completely)
            if (response is null)
            {
                _logger.LogError("Navigation returned null response for {Url}", url);
                var screenshot = await _screenshotLogger.CaptureErrorAsync(page, url, "null-response", consoleMessages);
                return CrawlResult.Failed(url, Errors.NavigationFailed, screenshot);
            }

            // Check HTTP status
            var statusCode = response.Status;
            _logger.LogDebug("HTTP {StatusCode} received for {Url}", statusCode, url);

            if (!response.Ok)
            {
                _logger.LogWarning("HTTP error {StatusCode} for {Url}", statusCode, url);
                var screenshot = await _screenshotLogger.CaptureErrorAsync(page, url, $"http-{statusCode}", consoleMessages);
                return CrawlResult.Failed(url, statusCode, screenshot);
            }

            // Check for empty content
            var html = await page.ContentAsync();
            _logger.LogDebug("Content retrieved: {Length} bytes for {Url}", html.Length, url);

            if (string.IsNullOrWhiteSpace(html))
            {
                _logger.LogWarning("Empty content received for {Url}", url);
                var screenshot = await _screenshotLogger.CaptureErrorAsync(page, url, "empty-content", consoleMessages);
                return CrawlResult.Failed(url, Errors.EmptyContent, screenshot);
            }

            // Check for very small content (likely error page)
            if (html.Length < 500)
            {
                _logger.LogWarning("Suspiciously small content ({Length} bytes) for {Url} - may be error page", html.Length, url);
            }

            // Check if body is empty or has only whitespace
            var bodyContent = await page.EvaluateAsync<string>("() => document.body?.innerText?.trim() || ''");
            _logger.LogDebug("Body text content: {Length} chars for {Url}", bodyContent.Length, url);

            if (string.IsNullOrWhiteSpace(bodyContent))
            {
                _logger.LogWarning("Page body text is empty for {Url}, may be JavaScript-rendered or blocked", url);
            }

            // Get final URL (after redirects)
            var finalUrl = page.Url;
            if (finalUrl != url)
            {
                _logger.LogInformation("URL redirected: {OriginalUrl} -> {FinalUrl}", url, finalUrl);
            }

            // Get page title for logging
            var title = await page.TitleAsync();
            _logger.LogDebug("Page title: '{Title}' for {Url}", title, url);

            // Success - NO screenshot (only screenshot on errors)
            _logger.LogInformation(
                "Successfully fetched {Url}: {HtmlLength} bytes HTML, {TextLength} chars text, {ElapsedMs}ms total, title='{Title}'",
                url, html.Length, bodyContent.Length, stopwatch.ElapsedMilliseconds, title);

            // Log console message summary if any
            if (consoleMessages.Count > 0)
            {
                var errorCount = consoleMessages.Count(m => m.StartsWith("[error]"));
                var warnCount = consoleMessages.Count(m => m.StartsWith("[warning]"));
                _logger.LogDebug("Console messages for {Url}: {Total} total, {Errors} errors, {Warnings} warnings",
                    url, consoleMessages.Count, errorCount, warnCount);
            }

            return CrawlResult.Success(url, html);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout after {TimeoutMs}ms fetching {Url}", _timeoutMs, url);
            var screenshot = await _screenshotLogger.CaptureErrorAsync(page, url, "timeout", consoleMessages);
            return CrawlResult.Failed(url, ex, screenshot);
        }
        catch (PlaywrightException ex)
        {
            _logger.LogError(ex, "Playwright error fetching {Url}: {Message}", url, ex.Message);
            var screenshot = await _screenshotLogger.CaptureErrorAsync(page, url, "playwright-error", consoleMessages);
            return CrawlResult.Failed(url, ex, screenshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching {Url}: {ExceptionType} - {Message}", 
                url, ex.GetType().Name, ex.Message);
            var screenshot = await _screenshotLogger.CaptureErrorAsync(page, url, "exception", consoleMessages);
            return CrawlResult.Failed(url, ex, screenshot);
        }
        finally
        {
            _logger.LogDebug("Closing page for {Url}", url);
            await page.CloseAsync();
            _logger.LogDebug("Page closed for {Url}", url);
        }
    }

    private async Task EnsureBrowserAsync()
    {
        if (_browser is not null)
        {
            _logger.LogDebug("Reusing existing browser instance");
            return;
        }

        _logger.LogInformation("Launching headless Chromium browser");
        
        var stopwatch = Stopwatch.StartNew();
        _playwright = await Playwright.CreateAsync();
        _logger.LogDebug("Playwright created in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

        stopwatch.Restart();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true, // Required for Azure deployment
            Args = new[]
            {
                "--disable-gpu",
                "--disable-dev-shm-usage", // Prevent /dev/shm issues in containers
                "--no-sandbox", // Required for some Azure environments
                "--disable-setuid-sandbox",
                "--disable-background-networking",
                "--disable-default-apps",
                "--disable-extensions",
                "--disable-sync",
                "--disable-translate",
                "--metrics-recording-only",
                "--mute-audio",
                "--no-first-run",
                "--safebrowsing-disable-auto-update"
            }
        });

        _logger.LogInformation("Headless Chromium browser launched successfully in {ElapsedMs}ms", 
            stopwatch.ElapsedMilliseconds);
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogDebug("Disposing WebCrawler");
        
        if (_browser is not null)
        {
            _logger.LogDebug("Closing browser");
            await _browser.CloseAsync();
            _logger.LogDebug("Browser closed");
        }
        
        if (_playwright is not null)
        {
            _playwright.Dispose();
            _logger.LogDebug("Playwright disposed");
        }
        
        _logger.LogInformation("WebCrawler disposed");
    }
}
