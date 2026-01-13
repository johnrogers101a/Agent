using System.Text.RegularExpressions;

namespace Crawl4AI.Algorithms;

/// <summary>
/// Porter Stemmer implementation for English text.
/// Reduces words to their root form for better matching.
/// </summary>
public sealed partial class EnglishStemmer
{
    /// <summary>
    /// Stems a single word to its root form.
    /// </summary>
    public string Stem(string word)
    {
        if (string.IsNullOrWhiteSpace(word) || word.Length < 3)
            return word.ToLowerInvariant();

        word = word.ToLowerInvariant();

        // Step 1a
        word = Step1a(word);
        // Step 1b
        word = Step1b(word);
        // Step 1c
        word = Step1c(word);
        // Step 2
        word = Step2(word);
        // Step 3
        word = Step3(word);
        // Step 4
        word = Step4(word);
        // Step 5
        word = Step5(word);

        return word;
    }

    /// <summary>
    /// Stems all words in the input tokens.
    /// </summary>
    public string[] StemAll(string[] tokens)
    {
        return tokens.Select(Stem).ToArray();
    }

    private static string Step1a(string word)
    {
        if (word.EndsWith("sses"))
            return word[..^2];
        if (word.EndsWith("ies"))
            return word[..^2];
        if (word.EndsWith("ss"))
            return word;
        if (word.EndsWith("s"))
            return word[..^1];
        return word;
    }

    private static string Step1b(string word)
    {
        if (word.EndsWith("eed"))
        {
            var stem = word[..^3];
            if (GetMeasure(stem) > 0)
                return stem + "ee";
            return word;
        }

        var modified = false;
        if (word.EndsWith("ed") && ContainsVowel(word[..^2]))
        {
            word = word[..^2];
            modified = true;
        }
        else if (word.EndsWith("ing") && ContainsVowel(word[..^3]))
        {
            word = word[..^3];
            modified = true;
        }

        if (modified)
        {
            if (word.EndsWith("at") || word.EndsWith("bl") || word.EndsWith("iz"))
                return word + "e";
            if (EndsWithDoubleConsonant(word) && !word.EndsWith("l") && !word.EndsWith("s") && !word.EndsWith("z"))
                return word[..^1];
            if (GetMeasure(word) == 1 && EndsWithCvc(word))
                return word + "e";
        }

        return word;
    }

    private static string Step1c(string word)
    {
        if (word.EndsWith("y") && ContainsVowel(word[..^1]))
            return word[..^1] + "i";
        return word;
    }

    private static string Step2(string word)
    {
        var suffixes = new Dictionary<string, string>
        {
            ["ational"] = "ate",
            ["tional"] = "tion",
            ["enci"] = "ence",
            ["anci"] = "ance",
            ["izer"] = "ize",
            ["abli"] = "able",
            ["alli"] = "al",
            ["entli"] = "ent",
            ["eli"] = "e",
            ["ousli"] = "ous",
            ["ization"] = "ize",
            ["ation"] = "ate",
            ["ator"] = "ate",
            ["alism"] = "al",
            ["iveness"] = "ive",
            ["fulness"] = "ful",
            ["ousness"] = "ous",
            ["aliti"] = "al",
            ["iviti"] = "ive",
            ["biliti"] = "ble"
        };

        foreach (var (suffix, replacement) in suffixes)
        {
            if (word.EndsWith(suffix))
            {
                var stem = word[..^suffix.Length];
                if (GetMeasure(stem) > 0)
                    return stem + replacement;
                return word;
            }
        }

        return word;
    }

    private static string Step3(string word)
    {
        var suffixes = new Dictionary<string, string>
        {
            ["icate"] = "ic",
            ["ative"] = "",
            ["alize"] = "al",
            ["iciti"] = "ic",
            ["ical"] = "ic",
            ["ful"] = "",
            ["ness"] = ""
        };

        foreach (var (suffix, replacement) in suffixes)
        {
            if (word.EndsWith(suffix))
            {
                var stem = word[..^suffix.Length];
                if (GetMeasure(stem) > 0)
                    return stem + replacement;
                return word;
            }
        }

        return word;
    }

    private static string Step4(string word)
    {
        var suffixes = new[]
        {
            "al", "ance", "ence", "er", "ic", "able", "ible", "ant", "ement",
            "ment", "ent", "ion", "ou", "ism", "ate", "iti", "ous", "ive", "ize"
        };

        foreach (var suffix in suffixes)
        {
            if (word.EndsWith(suffix))
            {
                var stem = word[..^suffix.Length];
                if (GetMeasure(stem) > 1)
                {
                    if (suffix == "ion" && stem.Length > 0 && (stem.EndsWith("s") || stem.EndsWith("t")))
                        return stem;
                    if (suffix != "ion")
                        return stem;
                }
                return word;
            }
        }

        return word;
    }

    private static string Step5(string word)
    {
        if (word.EndsWith("e"))
        {
            var stem = word[..^1];
            if (GetMeasure(stem) > 1)
                return stem;
            if (GetMeasure(stem) == 1 && !EndsWithCvc(stem))
                return stem;
        }

        if (word.EndsWith("ll") && GetMeasure(word[..^1]) > 1)
            return word[..^1];

        return word;
    }

    private static int GetMeasure(string word)
    {
        // Count VC sequences
        var measure = 0;
        var inVowel = false;

        foreach (var c in word)
        {
            var isVowel = IsVowel(c);
            if (!inVowel && isVowel)
                inVowel = true;
            else if (inVowel && !isVowel)
            {
                measure++;
                inVowel = false;
            }
        }

        return measure;
    }

    private static bool ContainsVowel(string word)
    {
        return word.Any(IsVowel);
    }

    private static bool IsVowel(char c)
    {
        return c is 'a' or 'e' or 'i' or 'o' or 'u';
    }

    private static bool EndsWithDoubleConsonant(string word)
    {
        if (word.Length < 2) return false;
        var last = word[^1];
        var secondLast = word[^2];
        return last == secondLast && !IsVowel(last);
    }

    private static bool EndsWithCvc(string word)
    {
        if (word.Length < 3) return false;
        var c1 = word[^3];
        var v = word[^2];
        var c2 = word[^1];

        return !IsVowel(c1) && IsVowel(v) && !IsVowel(c2) && c2 is not 'w' and not 'x' and not 'y';
    }
}

/// <summary>
/// Token cleaning utilities.
/// </summary>
public static class TokenCleaner
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "and", "are", "as", "at", "be", "by", "for", "from",
        "has", "he", "in", "is", "it", "its", "of", "on", "that", "the",
        "to", "was", "were", "will", "with", "the", "this", "but", "they",
        "have", "had", "what", "when", "where", "who", "which", "why", "how"
    };

    /// <summary>
    /// Tokenizes text into words, removing punctuation and converting to lowercase.
    /// </summary>
    public static string[] Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<string>();

        return Regex.Split(text.ToLowerInvariant(), @"\W+")
            .Where(t => t.Length > 1)
            .ToArray();
    }

    /// <summary>
    /// Removes stop words and short tokens from the token list.
    /// </summary>
    public static string[] CleanTokens(string[] tokens)
    {
        return tokens
            .Where(t => t.Length > 2 && !StopWords.Contains(t))
            .ToArray();
    }
}
