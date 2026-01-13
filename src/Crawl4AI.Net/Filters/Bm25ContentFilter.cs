using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Crawl4AI.Abstractions;
using Crawl4AI.Algorithms;
using Crawl4AI.Models;

namespace Crawl4AI.Filters;

/// <summary>
/// Content filtering using BM25 algorithm with priority tag handling.
/// Ranks content chunks by relevance to a user query or page metadata.
/// </summary>
public sealed class Bm25ContentFilter : IContentFilter
{
    private readonly Bm25FilterOptions _options;
    private readonly EnglishStemmer? _stemmer;

    /// <summary>
    /// Priority tag weights - multipliers for BM25 scores.
    /// </summary>
    private static readonly Dictionary<string, double> PriorityTags = new(StringComparer.OrdinalIgnoreCase)
    {
        ["h1"] = 5.0,
        ["h2"] = 4.0,
        ["h3"] = 3.0,
        ["title"] = 4.0,
        ["strong"] = 2.0,
        ["b"] = 1.5,
        ["em"] = 1.5,
        ["blockquote"] = 2.0,
        ["code"] = 2.0,
        ["pre"] = 1.5,
        ["th"] = 1.5
    };

    /// <summary>
    /// Tags to extract text chunks from.
    /// </summary>
    private static readonly HashSet<string> ContentTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "h1", "h2", "h3", "h4", "h5", "h6", "li", "td", "th",
        "blockquote", "pre", "code", "article", "section", "div"
    };

    public Bm25ContentFilter() : this(new Bm25FilterOptions()) { }

    public Bm25ContentFilter(Bm25FilterOptions options)
    {
        _options = options;
        _stemmer = options.UseStemming ? new EnglishStemmer() : null;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> FilterContent(string html, int? minWordThreshold = null)
    {
        if (string.IsNullOrWhiteSpace(html))
            return Array.Empty<string>();

        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        var body = document.Body;
        if (body is null)
        {
            document = parser.ParseDocument($"<body>{html}</body>");
            body = document.Body!;
        }

        // Get query - from options or extract from page metadata
        var query = _options.UserQuery ?? ExtractPageQuery(document, body);
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<string>();

        // Extract text chunks
        var candidates = ExtractTextChunks(body, minWordThreshold);
        if (candidates.Count == 0)
            return Array.Empty<string>();

        // Tokenize corpus and query
        var tokenizedCorpus = candidates
            .Select(c => TokenizeText(c.Text))
            .ToList();

        var tokenizedQuery = TokenizeText(query);

        // Clean tokens (remove stop words)
        tokenizedCorpus = tokenizedCorpus.Select(TokenCleaner.CleanTokens).ToList();
        tokenizedQuery = TokenCleaner.CleanTokens(tokenizedQuery);

        if (tokenizedQuery.Length == 0)
            return Array.Empty<string>();

        // Compute BM25 scores
        var bm25 = new Bm25Okapi(tokenizedCorpus);
        var scores = bm25.GetScores(tokenizedQuery);

        // Adjust scores with tag weights
        var scoredCandidates = candidates
            .Select((chunk, i) => (
                Score: scores[i] * PriorityTags.GetValueOrDefault(chunk.TagType, 1.0),
                Chunk: chunk
            ))
            .Where(x => x.Score >= _options.Threshold)
            .OrderBy(x => x.Chunk.Index) // Maintain document order
            .Select(x => x.Chunk.Html)
            .ToList();

        return scoredCandidates;
    }

    private string[] TokenizeText(string text)
    {
        var tokens = TokenCleaner.Tokenize(text);
        
        if (_stemmer is not null)
            tokens = _stemmer.StemAll(tokens);

        return tokens;
    }

    private static string? ExtractPageQuery(IDocument document, IElement body)
    {
        // Try title
        var title = document.Title;
        if (!string.IsNullOrWhiteSpace(title))
            return title;

        // Try meta description
        var metaDesc = document.QuerySelector("meta[name='description']")?.GetAttribute("content");
        if (!string.IsNullOrWhiteSpace(metaDesc))
            return metaDesc;

        // Try meta keywords
        var metaKeywords = document.QuerySelector("meta[name='keywords']")?.GetAttribute("content");
        if (!string.IsNullOrWhiteSpace(metaKeywords))
            return metaKeywords;

        // Try first h1
        var h1 = body.QuerySelector("h1")?.TextContent?.Trim();
        if (!string.IsNullOrWhiteSpace(h1))
            return h1;

        // Try first significant paragraph
        var firstP = body.QuerySelector("p")?.TextContent?.Trim();
        if (!string.IsNullOrWhiteSpace(firstP) && firstP.Length > 50)
            return firstP[..Math.Min(200, firstP.Length)];

        return null;
    }

    private List<TextChunk> ExtractTextChunks(IElement body, int? minWordThreshold)
    {
        var chunks = new List<TextChunk>();
        var index = 0;

        void ExtractFromElement(IElement element)
        {
            foreach (var child in element.Children)
            {
                if (ContentTags.Contains(child.LocalName))
                {
                    var text = child.TextContent?.Trim() ?? "";
                    var wordCount = text.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries).Length;

                    if (wordCount >= (minWordThreshold ?? 3))
                    {
                        chunks.Add(new TextChunk
                        {
                            Index = index++,
                            Text = text,
                            TagType = child.LocalName,
                            Html = CleanElement(child)
                        });
                    }
                }

                ExtractFromElement(child);
            }
        }

        ExtractFromElement(body);
        return chunks;
    }

    private static string CleanElement(IElement element)
    {
        // Clone and clean the element
        var clone = element.Clone() as IElement;
        if (clone is null)
            return element.TextContent ?? "";

        // Remove script, style, etc.
        foreach (var unwanted in clone.QuerySelectorAll("script, style, noscript, iframe").ToList())
        {
            unwanted.Remove();
        }

        return clone.OuterHtml;
    }
}
