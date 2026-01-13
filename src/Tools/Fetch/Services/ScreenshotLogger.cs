using Fetch.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace Fetch.Services;

/// <summary>
/// Captures diagnostic screenshots ONLY on errors for debugging page load failures.
/// </summary>
public sealed class ScreenshotLogger
{
    private readonly ILogger<ScreenshotLogger> _logger;
    private readonly string _outputDir;
    private readonly int _maxScreenshots;

    public ScreenshotLogger(ILogger<ScreenshotLogger> logger, IConfiguration config)
    {
        _logger = logger;
        
        var configuredDir = config[ConfigKeys.ScreenshotDir];
        _outputDir = !string.IsNullOrEmpty(configuredDir)
            ? configuredDir
            : Path.Combine(Path.GetTempPath(), Defaults.ScreenshotDir);
        
        _maxScreenshots = config.GetValue(ConfigKeys.MaxScreenshots, Defaults.MaxScreenshots);

        // Ensure directory exists
        Directory.CreateDirectory(_outputDir);
        
        _logger.LogInformation("ScreenshotLogger initialized: dir={OutputDir}, maxScreenshots={MaxScreenshots}", 
            _outputDir, _maxScreenshots);
    }

    /// <summary>
    /// Captures a diagnostic screenshot on ERROR only.
    /// </summary>
    /// <param name="page">The Playwright page to capture.</param>
    /// <param name="url">The URL being loaded.</param>
    /// <param name="errorLabel">Label describing the error type.</param>
    /// <param name="consoleMessages">Browser console messages captured during loading.</param>
    /// <returns>Screenshot info with file path and metadata.</returns>
    public async Task<ScreenshotInfo> CaptureErrorAsync(
        IPage page,
        string url,
        string errorLabel,
        List<string>? consoleMessages = null)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
        var safeHost = SanitizeFilename(GetHostFromUrl(url));
        var safeLabel = SanitizeFilename(errorLabel);
        var filename = $"{timestamp}_{safeHost}_{safeLabel}.png";
        var filepath = Path.Combine(_outputDir, filename);

        _logger.LogDebug("Capturing error screenshot for {Url} with label '{Label}'", url, errorLabel);
        _logger.LogDebug("Screenshot path: {FilePath}", filepath);

        try
        {
            // Capture full page screenshot
            _logger.LogDebug("Taking screenshot...");
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = filepath,
                FullPage = true,
                Type = ScreenshotType.Png
            });
            _logger.LogDebug("Screenshot captured: {FilePath}", filepath);

            // Gather metadata
            var pageTitle = await page.TitleAsync();
            var currentUrl = page.Url;

            _logger.LogDebug("Page metadata - Title: '{Title}', CurrentUrl: {CurrentUrl}", pageTitle, currentUrl);

            var info = new ScreenshotInfo
            {
                FilePath = filepath,
                Url = url,
                Label = errorLabel,
                Timestamp = DateTime.UtcNow,
                PageTitle = pageTitle,
                CurrentUrl = currentUrl,
                ConsoleMessages = consoleMessages ?? new()
            };

            // Log summary at warning level (errors get screenshots)
            var consoleErrorCount = consoleMessages?.Count(m => m.StartsWith("[error]", StringComparison.OrdinalIgnoreCase)) ?? 0;
            var consoleWarnCount = consoleMessages?.Count(m => m.StartsWith("[warning]", StringComparison.OrdinalIgnoreCase)) ?? 0;

            _logger.LogWarning(
                "Error screenshot saved: {FilePath} | URL: {Url} | Title: '{Title}' | Label: {Label} | Console: {ErrorCount} errors, {WarnCount} warnings",
                filepath, url, pageTitle, errorLabel, consoleErrorCount, consoleWarnCount);

            // Log all console messages at debug level
            if (consoleMessages?.Count > 0)
            {
                _logger.LogDebug("Console messages captured for {Url} ({Count} total):", url, consoleMessages.Count);
                
                var messageIndex = 0;
                foreach (var msg in consoleMessages.Take(50)) // Limit logged messages
                {
                    _logger.LogDebug("  [{Index}] {Message}", messageIndex++, msg);
                }
                
                if (consoleMessages.Count > 50)
                {
                    _logger.LogDebug("  ... and {RemainingCount} more messages", consoleMessages.Count - 50);
                }
            }
            else
            {
                _logger.LogDebug("No console messages captured for {Url}", url);
            }

            // Cleanup old screenshots
            await CleanupOldScreenshotsAsync();

            return info;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture screenshot for {Url}: {Message}", url, ex.Message);
            
            return new ScreenshotInfo
            {
                Url = url,
                Label = errorLabel,
                Error = $"Screenshot capture failed: {ex.Message}",
                Timestamp = DateTime.UtcNow,
                ConsoleMessages = consoleMessages ?? new()
            };
        }
    }

    private async Task CleanupOldScreenshotsAsync()
    {
        try
        {
            _logger.LogDebug("Checking for old screenshots to cleanup (max: {MaxScreenshots})", _maxScreenshots);

            var files = Directory.GetFiles(_outputDir, "*.png")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTimeUtc)
                .Skip(_maxScreenshots)
                .ToList();

            if (files.Count == 0)
            {
                _logger.LogDebug("No old screenshots to cleanup");
                return;
            }

            _logger.LogDebug("Cleaning up {Count} old screenshots (keeping newest {MaxScreenshots})", 
                files.Count, _maxScreenshots);

            var deletedCount = 0;
            var failedCount = 0;

            foreach (var file in files)
            {
                try
                {
                    var fileName = file.Name;
                    file.Delete();
                    deletedCount++;
                    _logger.LogDebug("Deleted old screenshot: {FileName}", fileName);
                }
                catch (Exception ex)
                {
                    failedCount++;
                    _logger.LogWarning(ex, "Failed to delete screenshot: {FilePath}", file.FullName);
                }
            }

            _logger.LogInformation("Screenshot cleanup complete: {DeletedCount} deleted, {FailedCount} failed",
                deletedCount, failedCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during screenshot cleanup in {OutputDir}", _outputDir);
        }

        await Task.CompletedTask; // Keep async signature for future enhancements
    }

    private static string GetHostFromUrl(string url)
    {
        try
        {
            return new Uri(url).Host;
        }
        catch
        {
            return "unknown-host";
        }
    }

    private static string SanitizeFilename(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "unknown";

        return new string(input
            .Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_')
            .Take(50) // Limit length
            .ToArray());
    }
}
