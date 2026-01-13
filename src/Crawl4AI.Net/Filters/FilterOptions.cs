namespace Crawl4AI.Filters;

/// <summary>
/// Threshold calculation mode for PruningContentFilter.
/// </summary>
public enum ThresholdType
{
    /// <summary>
    /// Fixed threshold - nodes scoring below threshold are removed.
    /// </summary>
    Fixed,

    /// <summary>
    /// Dynamic threshold - adjusts based on tag importance, text ratio, and link ratio.
    /// </summary>
    Dynamic
}

/// <summary>
/// Configuration options for PruningContentFilter.
/// </summary>
public sealed record PruningFilterOptions
{
    /// <summary>
    /// Base threshold for content relevance (0-1). Default: 0.48
    /// </summary>
    public double Threshold { get; init; } = 0.48;

    /// <summary>
    /// Threshold calculation mode. Default: Fixed
    /// </summary>
    public ThresholdType ThresholdType { get; init; } = ThresholdType.Fixed;

    /// <summary>
    /// Minimum words required for a block to be kept. Default: null (no minimum)
    /// </summary>
    public int? MinWordThreshold { get; init; }
}

/// <summary>
/// Configuration options for BM25ContentFilter.
/// </summary>
public sealed record Bm25FilterOptions
{
    /// <summary>
    /// Search query for relevance filtering. If null, uses page metadata.
    /// </summary>
    public string? UserQuery { get; init; }

    /// <summary>
    /// Minimum BM25 score for a chunk to be included. Default: 1.0
    /// </summary>
    public double Threshold { get; init; } = 1.0;

    /// <summary>
    /// Language for stemming (e.g., "english"). Default: "english"
    /// </summary>
    public string Language { get; init; } = "english";

    /// <summary>
    /// Whether to apply stemming to query and content. Default: true
    /// </summary>
    public bool UseStemming { get; init; } = true;
}
