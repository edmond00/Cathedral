using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Catalyst;
using Mosaik.Core;

namespace Cathedral.Game.Narrative;

/// <summary>
/// English noun extractor. Uses Catalyst POS tagging when initialized,
/// falling back to rule-based heuristics otherwise.
/// </summary>
public static class NounExtractor
{
    private static Pipeline? _pipeline;
    private static bool _initialized = false;

    // ── Initialisation ─────────────────────────────────────────────────────────

    /// <summary>
    /// Loads the Catalyst English model. Call once from KeywordFallbackService.InitializeAsync.
    /// Subsequent calls to ExtractNouns will use POS tagging instead of heuristics.
    /// </summary>
    public static async Task InitializeAsync(string modelStoragePath)
    {
        if (_initialized) return;
        try
        {
            Catalyst.Models.English.Register();
            Storage.Current = new DiskStorage(modelStoragePath);
            _pipeline = await Pipeline.ForAsync(Language.English);
            _initialized = true;
            Console.WriteLine("NounExtractor: Catalyst POS pipeline ready.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NounExtractor: Catalyst init failed, using rule-based fallback. {ex.Message}");
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Extracts candidate nouns from the given text.
    /// Uses Catalyst POS tagging if initialized, rule-based heuristics as fallback.
    /// Returns distinct lowercase words.
    /// </summary>
    public static List<string> ExtractNouns(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        if (_initialized && _pipeline != null)
            return ExtractNounsCatalyst(text);

        return ExtractNounsRuleBased(text);
    }

    // ── Catalyst path ──────────────────────────────────────────────────────────

    private static List<string> ExtractNounsCatalyst(string text)
    {
        try
        {
            var doc = new Document(text, Language.English);
            _pipeline!.ProcessSingle(doc);

            var nouns = new List<string>();
            var seen  = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var span in doc)
            {
                foreach (var token in span)
                {
                    if (token.POS is PartOfSpeech.NOUN or PartOfSpeech.PROPN)
                    {
                        var lower = token.Value.ToLowerInvariant();
                        if (lower.Length >= 3 && !StopWords.Contains(lower) && seen.Add(lower))
                            nouns.Add(lower);
                    }
                }
            }

            if (nouns.Count > 0)
                return nouns;

            // If Catalyst found nothing, fall back to rule-based
            Console.WriteLine("NounExtractor: Catalyst found no nouns, falling back to rule-based.");
            return ExtractNounsRuleBased(text);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NounExtractor: Catalyst extraction failed: {ex.Message}");
            return ExtractNounsRuleBased(text);
        }
    }

    // ── Rule-based fallback ────────────────────────────────────────────────────

    private static readonly HashSet<string> Determiners = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "this", "that", "these", "those",
        "my", "your", "his", "her", "its", "our", "their",
        "some", "any", "each", "every", "no", "both", "all",
        "another", "other", "several", "many", "few", "much",
        "which", "what", "whose"
    };

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "this", "that", "these", "those",
        "my", "your", "his", "her", "its", "our", "their",
        "some", "any", "each", "every", "no", "both", "all",
        "another", "other", "several", "many", "few", "much",
        "which", "what", "whose",
        "i", "me", "we", "us", "you", "he", "she", "it", "they", "them",
        "who", "whom", "one",
        "am", "is", "are", "was", "were", "be", "been", "being",
        "have", "has", "had", "having",
        "do", "does", "did",
        "will", "would", "shall", "should",
        "may", "might", "must", "can", "could",
        "in", "on", "at", "to", "for", "of", "with", "by", "from",
        "as", "into", "through", "over", "under", "between", "among",
        "before", "after", "above", "below", "up", "down", "out",
        "around", "along", "near", "off", "about", "against", "across",
        "toward", "towards", "upon", "within", "without", "beyond",
        "beside", "besides", "behind", "beneath",
        "and", "or", "but", "nor", "yet", "so",
        "although", "because", "since", "though", "unless", "until",
        "while", "if", "when", "where", "how", "whether", "than",
        "not", "just", "now", "then", "here", "there", "also",
        "very", "too", "more", "most", "less", "least",
        "well", "still", "even", "only", "already", "always", "never",
        "often", "sometimes", "usually", "again", "once", "twice",
        "rather", "quite", "almost", "enough", "indeed", "perhaps",
        "maybe", "soon", "far", "away", "back", "forward",
        "like", "such", "own", "same", "new", "old", "good", "bad",
        "big", "small", "large", "little", "long", "short", "high", "low",
        "next", "last", "first", "second", "third", "right", "left",
        "open", "close", "hard", "soft", "cold", "hot", "warm", "cool",
        "dark", "bright", "light", "deep", "wide", "narrow",
        "dry", "wet", "bare", "thick", "thin", "heavy", "rough",
        "get", "got", "make", "made", "take", "took", "come", "came",
        "go", "went", "see", "saw", "look", "know", "knew", "think",
        "thought", "find", "found", "give", "gave", "use", "used",
        "seem", "feel", "felt", "become", "became", "keep", "kept",
        "let", "put", "set", "run", "ran", "hold", "held", "bring",
        "brought", "show", "hear", "heard", "play", "move", "live",
        "try", "tried", "call", "ask", "turn", "need", "help",
        "start", "started", "stand", "stood", "lose", "lost", "pay",
        "meet", "met", "lie", "lay", "sit", "sat",
        "detect", "notice", "observe", "watch", "catch", "hitting",
        "glance", "glimpse", "perceive", "spot", "survey"
    };

    private static readonly (string Suffix, int MinLength)[] NounSuffixes =
    {
        ("tion",  7),
        ("sion",  7),
        ("ness",  7),
        ("ment",  7),
        ("ance",  7),
        ("ence",  7),
        ("ity",   6),
        ("ship",  7),
        ("hood",  7),
        ("dom",   6),
        ("ism",   6),
        ("ist",   6),
        ("ure",   6),
        ("age",   5),
        ("ling",  6),
        ("ock",   5),
        ("let",   5),
    };

    // Matches words of 2+ chars (lowered minimum to catch nouns after "a"/"an")
    private static readonly Regex WordPattern = new(@"[a-zA-Z]{2,}", RegexOptions.Compiled);

    private static List<string> ExtractNounsRuleBased(string text)
    {
        var tokens = WordPattern.Matches(text)
            .Cast<Match>()
            .Select(m => m.Value)
            .ToList();

        if (tokens.Count == 0)
            return new List<string>();

        var afterDeterminer = new List<string>();
        var suffixMatches   = new List<string>();

        for (int i = 0; i < tokens.Count; i++)
        {
            var lower = tokens[i].ToLowerInvariant();

            if (Determiners.Contains(lower))
            {
                for (int j = i + 1; j < tokens.Count; j++)
                {
                    var candidate = tokens[j].ToLowerInvariant();
                    if (!StopWords.Contains(candidate) && candidate.Length >= 3)
                    {
                        afterDeterminer.Add(candidate);
                        break;
                    }
                }
                continue;
            }

            if (StopWords.Contains(lower)) continue;

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

        var seen   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        foreach (var w in afterDeterminer.Concat(suffixMatches))
            if (seen.Add(w)) result.Add(w);

        // Last resort: any non-stop word of length >= 4
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
