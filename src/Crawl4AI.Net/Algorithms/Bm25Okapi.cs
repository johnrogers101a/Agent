namespace Crawl4AI.Algorithms;

/// <summary>
/// BM25 Okapi ranking algorithm implementation for text relevance scoring.
/// </summary>
public sealed class Bm25Okapi
{
    private readonly List<string[]> _corpus;
    private readonly Dictionary<string, double> _idf;
    private readonly double[] _docLengths;
    private readonly double _avgDocLength;
    private readonly double _k1;
    private readonly double _b;

    /// <summary>
    /// Creates a new BM25 index from a tokenized corpus.
    /// </summary>
    /// <param name="corpus">List of documents, each document is an array of tokens.</param>
    /// <param name="k1">Term frequency saturation parameter (default: 1.2).</param>
    /// <param name="b">Length normalization parameter (default: 0.75).</param>
    public Bm25Okapi(IEnumerable<string[]> corpus, double k1 = 1.2, double b = 0.75)
    {
        _corpus = corpus.ToList();
        _k1 = k1;
        _b = b;

        _docLengths = _corpus.Select(doc => (double)doc.Length).ToArray();
        _avgDocLength = _docLengths.Length > 0 ? _docLengths.Average() : 1.0;

        _idf = ComputeIdf();
    }

    /// <summary>
    /// Computes BM25 scores for all documents given a query.
    /// </summary>
    /// <param name="query">Tokenized query.</param>
    /// <returns>Array of scores, one per document in the corpus.</returns>
    public double[] GetScores(string[] query)
    {
        var scores = new double[_corpus.Count];

        for (var i = 0; i < _corpus.Count; i++)
        {
            scores[i] = ScoreDocument(_corpus[i], query, _docLengths[i]);
        }

        return scores;
    }

    private double ScoreDocument(string[] document, string[] query, double docLength)
    {
        var score = 0.0;
        var termFrequencies = ComputeTermFrequencies(document);

        foreach (var term in query.Distinct())
        {
            if (!_idf.TryGetValue(term, out var idf))
                continue;

            var tf = termFrequencies.GetValueOrDefault(term, 0);
            if (tf == 0) continue;

            var numerator = tf * (_k1 + 1);
            var denominator = tf + _k1 * (1 - _b + _b * (docLength / _avgDocLength));

            score += idf * (numerator / denominator);
        }

        return score;
    }

    private Dictionary<string, double> ComputeIdf()
    {
        var idf = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var docCount = _corpus.Count;

        // Count documents containing each term
        var docFrequencies = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var document in _corpus)
        {
            foreach (var term in document.Distinct())
            {
                docFrequencies[term] = docFrequencies.GetValueOrDefault(term, 0) + 1;
            }
        }

        // Compute IDF for each term
        foreach (var (term, df) in docFrequencies)
        {
            // IDF formula: log((N - df + 0.5) / (df + 0.5) + 1)
            idf[term] = Math.Log((docCount - df + 0.5) / (df + 0.5) + 1);
        }

        return idf;
    }

    private static Dictionary<string, int> ComputeTermFrequencies(string[] document)
    {
        var frequencies = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var term in document)
        {
            frequencies[term] = frequencies.GetValueOrDefault(term, 0) + 1;
        }
        return frequencies;
    }
}
