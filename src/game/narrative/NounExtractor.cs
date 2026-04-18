using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Lightweight English noun extractor based on grammatical heuristics.
/// Identifies candidate nouns from short text by combining:
///   - Context rules: words immediately following determiners/articles are likely nouns.
///   - Suffix rules: common English noun endings (-tion, -ness, -ity, -ment, etc.).
///   - Stop-word filtering: removes function words, auxiliaries, and pronouns.
///
/// This is a rule-based alternative to full POS-tagging libraries such as
/// Stanford.NLP.NET or Catalyst, chosen to avoid heavy dependencies for
/// short game text (1-3 sentences).  The LLM critic makes the final
/// selection from the candidates, so high recall matters more than precision.
/// Replace with a proper POS tagger if more accurate extraction is needed.
/// </summary>
public static class NounExtractor
{
    // ── Stop words ─────────────────────────────────────────────────────────────

    /// <summary>Words that immediately precede a noun (the next content word is likely a noun).</summary>
    private static readonly HashSet<string> Determiners = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "this", "that", "these", "those",
        "my", "your", "his", "her", "its", "our", "their",
        "some", "any", "each", "every", "no", "both", "all",
        "another", "other", "several", "many", "few", "much",
        "which", "what", "whose"
    };

    /// <summary>Function words that should never be returned as noun candidates.</summary>
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Articles / determiners (already in Determiners — also excluded from results)
        "a", "an", "the", "this", "that", "these", "those",
        "my", "your", "his", "her", "its", "our", "their",
        "some", "any", "each", "every", "no", "both", "all",
        "another", "other", "several", "many", "few", "much",
        "which", "what", "whose",
        // Pronouns
        "i", "me", "we", "us", "you", "he", "she", "it", "they", "them",
        "who", "whom", "one",
        // Auxiliary verbs
        "am", "is", "are", "was", "were", "be", "been", "being",
        "have", "has", "had", "having",
        "do", "does", "did",
        "will", "would", "shall", "should",
        "may", "might", "must", "can", "could",
        // Common prepositions
        "in", "on", "at", "to", "for", "of", "with", "by", "from",
        "as", "into", "through", "over", "under", "between", "among",
        "before", "after", "above", "below", "up", "down", "out",
        "around", "along", "near", "off", "about", "against", "across",
        "toward", "towards", "upon", "within", "without", "beyond",
        "beside", "besides", "behind", "beneath",
        // Conjunctions
        "and", "or", "but", "nor", "yet", "so",
        "although", "because", "since", "though", "unless", "until",
        "while", "if", "when", "where", "how", "whether", "than",
        // Adverbs & particles
        "not", "just", "now", "then", "here", "there", "also",
        "very", "too", "more", "most", "less", "least",
        "well", "still", "even", "only", "already", "always", "never",
        "often", "sometimes", "usually", "again", "once", "twice",
        "rather", "quite", "almost", "enough", "indeed", "perhaps",
        "maybe", "soon", "far", "away", "back", "forward",
        // Common adjectives that might be confused with nouns
        "like", "such", "own", "same", "new", "old", "good", "bad",
        "big", "small", "large", "little", "long", "short", "high", "low",
        "next", "last", "first", "second", "third", "right", "left",
        "open", "close", "hard", "soft", "cold", "hot", "warm", "cool",
        "dark", "bright", "light", "deep", "wide", "narrow",
        // Verb forms that appear without auxiliaries
        "get", "got", "make", "made", "take", "took", "come", "came",
        "go", "went", "see", "saw", "look", "know", "knew", "think",
        "thought", "find", "found", "give", "gave", "use", "used",
        "seem", "feel", "felt", "become", "became", "keep", "kept",
        "let", "put", "set", "run", "ran", "hold", "held", "bring",
        "brought", "show", "hear", "heard", "play", "move", "live",
        "try", "tried", "call", "ask", "turn", "need", "help",
        "start", "started", "stand", "stood", "lose", "lost", "pay",
        "meet", "met", "lie", "lay", "sit", "sat"
    };

    // ── Suffix rules ───────────────────────────────────────────────────────────

    /// <summary>Pairs of (suffix, minimum word length) that strongly indicate a noun.</summary>
    private static readonly (string Suffix, int MinLength)[] NounSuffixes =
    {
        ("tion",  7),  // observation, perception
        ("sion",  7),  // expansion, tension
        ("ness",  7),  // darkness, stillness
        ("ment",  7),  // movement, settlement
        ("ance",  7),  // distance, balance
        ("ence",  7),  // presence, silence
        ("ity",   6),  // clarity, gravity
        ("ship",  7),  // kinship, hardship
        ("hood",  7),  // likelihood
        ("dom",   6),  // kingdom, freedom
        ("ism",   6),  // organism
        ("ist",   6),  // botanist
        ("ure",   6),  // texture, moisture
        ("age",   5),  // foliage, passage
        ("ling",  6),  // nestling, seedling
        ("ock",   5),  // bullock
        ("let",   5),  // droplet, brooklet
    };

    // ── Tokeniser ──────────────────────────────────────────────────────────────

    /// <summary>Matches a sequence of alphabetic characters (pure word, no digits or punctuation).</summary>
    private static readonly Regex WordPattern = new(@"[a-zA-Z]{3,}", RegexOptions.Compiled);

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Extracts candidate nouns from the given English text.
    /// Returns distinct lowercase words, ordered with determiner-preceded words first.
    /// The list may be empty if no nouns can be identified.
    /// </summary>
    public static List<string> ExtractNouns(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        var tokens = WordPattern.Matches(text)
            .Cast<Match>()
            .Select(m => m.Value)
            .ToList();

        if (tokens.Count == 0)
            return new List<string>();

        var afterDeterminer = new List<string>();   // highest-confidence candidates
        var suffixMatches   = new List<string>();   // medium-confidence candidates

        for (int i = 0; i < tokens.Count; i++)
        {
            var lower = tokens[i].ToLowerInvariant();

            // --- Context rule: next meaningful word after a determiner ---
            if (Determiners.Contains(lower))
            {
                // Skip adjectives/adverbs between the determiner and the noun
                for (int j = i + 1; j < tokens.Count; j++)
                {
                    var candidate = tokens[j].ToLowerInvariant();
                    if (!StopWords.Contains(candidate))
                    {
                        afterDeterminer.Add(candidate);
                        break;
                    }
                }
                continue; // the determiner itself is never a noun candidate
            }

            // Skip function words for the remaining checks
            if (StopWords.Contains(lower)) continue;

            // --- Suffix rule ---
            foreach (var (suffix, minLen) in NounSuffixes)
            {
                if (lower.Length >= minLen &&
                    lower.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    suffixMatches.Add(lower);
                    break;
                }
            }
        }

        // Merge: determiner-preceded first, suffix-based second, deduplicated
        var seen   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        foreach (var w in afterDeterminer.Concat(suffixMatches))
        {
            if (seen.Add(w)) result.Add(w);
        }

        // Last resort: any non-stop word of length ≥ 4 that we haven't already added
        if (result.Count < 2)
        {
            foreach (var token in tokens)
            {
                var lower = token.ToLowerInvariant();
                if (lower.Length >= 4 && !StopWords.Contains(lower) && seen.Add(lower))
                    result.Add(lower);
            }
        }

        return result;
    }
}
