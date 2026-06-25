using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Picks the clickable keyword for an observation sentence by rule, not by LLM: extract the nouns
/// (with lemmas) from the final sanitized text, then choose the noun whose lemma is most
/// semantically related (FastText/GloVe cosine similarity) to the observation object's reference
/// lemma — excluding the object's own word so the keyword is something *associated with* the object.
/// Falls back to the longest noun (or longest word) when embeddings or nouns are unavailable.
/// </summary>
public static class KeywordExtractor
{
    public static async Task InitializeAsync(string modelStoragePath)
    {
        await NounExtractor.InitializeAsync(modelStoragePath);
        await WordEmbedding.InitializeAsync();
    }

    /// <summary>
    /// Returns the keyword (a surface noun from <paramref name="text"/>), or null only when the text
    /// has no usable word at all. <paramref name="referenceLemma"/> is the observation object's core noun.
    /// </summary>
    public static string? ExtractKeyword(string text, string referenceLemma)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var candidates = NounExtractor.ExtractNounsWithLemmas(text);
        var refLemma = (referenceLemma ?? string.Empty).ToLowerInvariant().Trim();

        // Drop the object's own word (by lemma) so the keyword is associated, not the object itself.
        if (candidates.Count > 1 && !string.IsNullOrEmpty(refLemma))
            candidates = candidates.Where(c => !c.Lemma.Equals(refLemma, StringComparison.OrdinalIgnoreCase)).ToList();

        // No nouns at all → longest word fallback.
        if (candidates.Count == 0)
            return LongestWord(text);

        // Embedding similarity: closest candidate lemma to the reference lemma.
        if (WordEmbedding.IsReady && !string.IsNullOrEmpty(refLemma))
        {
            var refVec = WordEmbedding.GetVector(refLemma);
            if (refVec != null)
            {
                string? best = null;
                double bestSim = double.NegativeInfinity;
                foreach (var c in candidates)
                {
                    var v = WordEmbedding.GetVector(c.Lemma);
                    if (v == null) continue;
                    double sim = WordEmbedding.Cosine(refVec, v);
                    if (sim > bestSim) { bestSim = sim; best = c.Surface; }
                }
                if (best != null) return best;
            }
        }

        // Fallback: longest noun surface.
        return candidates.OrderByDescending(c => c.Surface.Length).First().Surface;
    }

    private static string? LongestWord(string text)
    {
        string? longest = null;
        foreach (Match m in Regex.Matches(text, @"[A-Za-z]{3,}"))
        {
            var w = m.Value.ToLowerInvariant();
            if (longest == null || w.Length > longest.Length) longest = w;
        }
        return longest;
    }
}
