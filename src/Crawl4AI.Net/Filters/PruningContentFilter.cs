using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Crawl4AI.Abstractions;
using Crawl4AI.Models;

namespace Crawl4AI.Filters;

/// <summary>
/// Content filtering using pruning algorithm with text density, link density, and tag importance scoring.
/// Removes less relevant nodes based on composite metrics.
/// </summary>
public sealed class PruningContentFilter : IContentFilter
{
    private readonly PruningFilterOptions _options;

    /// <summary>
    /// Tag weights for scoring - higher weight means more important.
    /// </summary>
    private static readonly Dictionary<string, double> TagWeights = new(StringComparer.OrdinalIgnoreCase)
    {
        ["article"] = 1.5,
        ["main"] = 1.4,
        ["section"] = 1.3,
        ["p"] = 1.2,
        ["h1"] = 1.4,
        ["h2"] = 1.3,
        ["h3"] = 1.2,
        ["h4"] = 1.1,
        ["h5"] = 1.0,
        ["h6"] = 0.9,
        ["div"] = 0.7,
        ["span"] = 0.6,
        ["li"] = 0.5,
        ["ul"] = 0.5,
        ["ol"] = 0.5
    };

    /// <summary>
    /// Tag importance for dynamic threshold adjustment.
    /// </summary>
    private static readonly Dictionary<string, double> TagImportance = new(StringComparer.OrdinalIgnoreCase)
    {
        ["article"] = 1.5,
        ["main"] = 1.4,
        ["section"] = 1.3,
        ["p"] = 1.2,
        ["h1"] = 1.4,
        ["h2"] = 1.3,
        ["h3"] = 1.2,
        ["div"] = 0.7,
        ["span"] = 0.6
    };

    /// <summary>
    /// Metric weights for composite score calculation.
    /// </summary>
    private static readonly Dictionary<string, double> MetricWeights = new()
    {
        ["text_density"] = 0.4,
        ["link_density"] = 0.2,
        ["tag_weight"] = 0.2,
        ["class_id_weight"] = 0.1,
        ["text_length"] = 0.1
    };

    /// <summary>
    /// Class/ID patterns that indicate main content (positive weight).
    /// </summary>
    private static readonly string[] ContentPatterns = 
    {
        "content", "article", "main", "post", "entry", "text", "body", "story"
    };

    /// <summary>
    /// Class/ID patterns that indicate boilerplate (negative weight).
    /// </summary>
    private static readonly string[] BoilerplatePatterns = 
    {
        "sidebar", "nav", "navigation", "menu", "footer", "header", "comment", 
        "advertisement", "ad", "social", "share", "related", "widget", "banner"
    };

    /// <summary>
    /// Tags to remove entirely during cleaning.
    /// </summary>
    private static readonly HashSet<string> UnwantedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style", "noscript", "iframe", "svg", "canvas", "video", "audio",
        "form", "input", "button", "select", "textarea", "nav", "footer", "header",
        "aside", "template"
    };

    public PruningContentFilter() : this(new PruningFilterOptions()) { }

    public PruningContentFilter(PruningFilterOptions options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> FilterContent(string html, int? minWordThreshold = null)
    {
        if (string.IsNullOrWhiteSpace(html))
            return Array.Empty<string>();

        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        // Wrap in body if missing
        var body = document.Body;
        if (body is null)
        {
            document = parser.ParseDocument($"<body>{html}</body>");
            body = document.Body!;
        }

        // Remove comments and unwanted tags
        RemoveComments(document);
        RemoveUnwantedTags(document);

        // Prune tree recursively
        PruneTree(body, minWordThreshold ?? _options.MinWordThreshold);

        // Extract remaining content blocks
        var contentBlocks = new List<string>();
        ExtractContentBlocks(body, contentBlocks);

        return contentBlocks;
    }

    private void RemoveComments(IDocument document)
    {
        var comments = document.Descendants<IComment>().ToList();
        foreach (var comment in comments)
        {
            comment.Remove();
        }
    }

    private void RemoveUnwantedTags(IDocument document)
    {
        foreach (var tagName in UnwantedTags)
        {
            var elements = document.QuerySelectorAll(tagName).ToList();
            foreach (var element in elements)
            {
                element.Remove();
            }
        }
    }

    private void PruneTree(IElement node, int? minWordThreshold)
    {
        if (node is null) return;

        var metrics = ComputeMetrics(node);
        var score = ComputeCompositeScore(metrics, minWordThreshold);

        var shouldRemove = _options.ThresholdType == ThresholdType.Fixed
            ? score < _options.Threshold
            : ShouldRemoveDynamic(metrics, score);

        if (shouldRemove)
        {
            node.Remove();
            return;
        }

        // Recursively prune children
        var children = node.Children.ToList();
        foreach (var child in children)
        {
            PruneTree(child, minWordThreshold);
        }
    }

    private bool ShouldRemoveDynamic(ContentMetrics metrics, double score)
    {
        var tagImportance = TagImportance.GetValueOrDefault(metrics.TagName, 0.7);
        var textRatio = metrics.TextDensity;
        var linkRatio = metrics.LinkDensity;

        var threshold = _options.Threshold;
        
        // Adjust threshold based on tag importance
        if (tagImportance > 1)
            threshold *= 0.8;
        
        // Lower threshold for text-dense content
        if (textRatio > 0.4)
            threshold *= 0.9;
        
        // Raise threshold for link-heavy content
        if (linkRatio > 0.6)
            threshold *= 1.2;

        return score < threshold;
    }

    private ContentMetrics ComputeMetrics(IElement node)
    {
        var text = node.TextContent?.Trim() ?? "";
        var tagLength = node.OuterHtml.Length;
        var linkText = string.Join(" ", node.QuerySelectorAll("a")
            .Select(a => a.TextContent?.Trim() ?? ""));

        return new ContentMetrics
        {
            TagName = node.LocalName,
            TextLength = text.Length,
            TagLength = tagLength,
            LinkTextLength = linkText.Length,
            Text = text,
            ClassName = node.ClassName,
            Id = node.Id
        };
    }

    private double ComputeCompositeScore(ContentMetrics metrics, int? minWordThreshold)
    {
        // Check minimum word threshold
        if (minWordThreshold.HasValue && metrics.WordCount < minWordThreshold.Value)
            return -1.0; // Guaranteed removal

        var score = 0.0;
        var totalWeight = 0.0;

        // Text density score
        score += MetricWeights["text_density"] * metrics.TextDensity;
        totalWeight += MetricWeights["text_density"];

        // Link density score (inverted - lower link density is better)
        var linkScore = 1 - metrics.LinkDensity;
        score += MetricWeights["link_density"] * linkScore;
        totalWeight += MetricWeights["link_density"];

        // Tag weight score
        var tagScore = TagWeights.GetValueOrDefault(metrics.TagName, 0.5);
        score += MetricWeights["tag_weight"] * tagScore;
        totalWeight += MetricWeights["tag_weight"];

        // Class/ID weight score
        var classIdScore = ComputeClassIdWeight(metrics);
        score += MetricWeights["class_id_weight"] * Math.Max(0, classIdScore);
        totalWeight += MetricWeights["class_id_weight"];

        // Text length score (logarithmic)
        var lengthScore = Math.Log(metrics.TextLength + 1);
        score += MetricWeights["text_length"] * lengthScore;
        totalWeight += MetricWeights["text_length"];

        return totalWeight > 0 ? score / totalWeight : 0;
    }

    private static double ComputeClassIdWeight(ContentMetrics metrics)
    {
        var score = 0.0;
        var combined = $"{metrics.ClassName} {metrics.Id}".ToLowerInvariant();

        foreach (var pattern in ContentPatterns)
        {
            if (combined.Contains(pattern))
                score += 1.0;
        }

        foreach (var pattern in BoilerplatePatterns)
        {
            if (combined.Contains(pattern))
                score -= 1.0;
        }

        return score;
    }

    private static void ExtractContentBlocks(IElement node, List<string> blocks)
    {
        // If this is a leaf-like element with text, add it
        var text = node.TextContent?.Trim() ?? "";
        if (text.Length > 0 && !node.Children.Any())
        {
            blocks.Add(node.OuterHtml);
            return;
        }

        // Otherwise recurse into children
        foreach (var child in node.Children)
        {
            ExtractContentBlocks(child, blocks);
        }

        // Also add the node itself if it has direct text content
        var directText = string.Join("", node.ChildNodes
            .OfType<IText>()
            .Select(t => t.Text?.Trim() ?? ""));
        
        if (directText.Length > 20)
        {
            blocks.Add($"<{node.LocalName}>{directText}</{node.LocalName}>");
        }
    }
}
