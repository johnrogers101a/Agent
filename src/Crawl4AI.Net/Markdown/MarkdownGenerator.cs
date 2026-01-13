using AngleSharp.Html.Parser;
using Crawl4AI.Abstractions;
using ReverseMarkdown;

namespace Crawl4AI.Markdown;

/// <summary>
/// Generates markdown from HTML content with optional content filtering.
/// </summary>
public sealed class MarkdownGenerator : IMarkdownGenerator
{
    private readonly IContentFilter? _contentFilter;
    private readonly Config _converterConfig;

    /// <summary>
    /// Creates a new markdown generator without filtering.
    /// </summary>
    public MarkdownGenerator() : this(contentFilter: null) { }

    /// <summary>
    /// Creates a new markdown generator with an optional content filter.
    /// </summary>
    /// <param name="contentFilter">Filter to apply before markdown conversion.</param>
    /// <param name="options">Additional converter options.</param>
    public MarkdownGenerator(IContentFilter? contentFilter = null, MarkdownGeneratorOptions? options = null)
    {
        _contentFilter = contentFilter;
        options ??= new MarkdownGeneratorOptions();

        _converterConfig = new Config
        {
            UnknownTags = Config.UnknownTagsOption.PassThrough,
            RemoveComments = true,
            SmartHrefHandling = true,
            GithubFlavored = true,
            ListBulletChar = '-'
        };
    }

    /// <inheritdoc />
    public MarkdownResult Generate(string html, string? baseUrl = null)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return new MarkdownResult
            {
                RawMarkdown = "",
                FitMarkdown = null,
                FitHtml = null,
                Title = null
            };
        }

        // Extract title
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);
        var title = document.Title ?? document.QuerySelector("h1")?.TextContent?.Trim();

        // Generate raw markdown from full HTML
        var converter = new Converter(_converterConfig);
        var rawMarkdown = converter.Convert(html);
        rawMarkdown = CleanMarkdown(rawMarkdown);

        // Apply content filter if provided
        string? fitMarkdown = null;
        string? fitHtml = null;

        if (_contentFilter is not null)
        {
            var filteredChunks = _contentFilter.FilterContent(html);
            
            if (filteredChunks.Count > 0)
            {
                fitHtml = string.Join("\n", filteredChunks.Select(c => $"<div>{c}</div>"));
                fitMarkdown = converter.Convert(fitHtml);
                fitMarkdown = CleanMarkdown(fitMarkdown);
            }
        }

        // Extract references
        var references = ExtractReferences(document, baseUrl);

        return new MarkdownResult
        {
            RawMarkdown = rawMarkdown,
            FitMarkdown = fitMarkdown,
            FitHtml = fitHtml,
            Title = title,
            ReferencesMarkdown = references
        };
    }

    private static string CleanMarkdown(string markdown)
    {
        // Remove excessive blank lines
        var lines = markdown.Split('\n');
        var cleanedLines = new List<string>();
        var blankCount = 0;

        foreach (var line in lines)
        {
            var trimmed = line.TrimEnd();
            
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                blankCount++;
                if (blankCount <= 2) // Allow max 2 consecutive blank lines
                    cleanedLines.Add("");
            }
            else
            {
                blankCount = 0;
                cleanedLines.Add(trimmed);
            }
        }

        return string.Join("\n", cleanedLines).Trim();
    }

    private static string? ExtractReferences(AngleSharp.Dom.IDocument document, string? baseUrl)
    {
        var links = document.QuerySelectorAll("a[href]")
            .Select(a => new
            {
                Text = a.TextContent?.Trim(),
                Href = a.GetAttribute("href")
            })
            .Where(l => !string.IsNullOrWhiteSpace(l.Text) && !string.IsNullOrWhiteSpace(l.Href))
            .DistinctBy(l => l.Href)
            .Take(50) // Limit references
            .ToList();

        if (links.Count == 0)
            return null;

        var references = new List<string> { "## References", "" };
        var index = 1;

        foreach (var link in links)
        {
            var href = link.Href!;
            
            // Resolve relative URLs
            if (!href.StartsWith("http") && !string.IsNullOrEmpty(baseUrl))
            {
                try
                {
                    href = new Uri(new Uri(baseUrl), href).ToString();
                }
                catch
                {
                    // Keep original href if resolution fails
                }
            }

            references.Add($"[{index}] [{link.Text}]({href})");
            index++;
        }

        return string.Join("\n", references);
    }
}

/// <summary>
/// Configuration options for markdown generation.
/// </summary>
public sealed record MarkdownGeneratorOptions
{
    /// <summary>
    /// Whether to ignore links in markdown output.
    /// </summary>
    public bool IgnoreLinks { get; init; }

    /// <summary>
    /// Whether to ignore images in markdown output.
    /// </summary>
    public bool IgnoreImages { get; init; }
}
