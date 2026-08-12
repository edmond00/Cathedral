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
        var kws = ExtractKeywords(text, referenceLemma, 1);
        return kws.Count > 0 ? kws[0] : null;
    }

    /// <summary>
    /// Returns up to <paramref name="count"/> distinct keywords (surface nouns) from
    /// <paramref name="text"/>, ranked by semantic relatedness to <paramref name="referenceLemma"/>
    /// (the object's core noun) and excluding the object's own word. Distinct by case-insensitive
    /// surface, so the same word is never returned twice; fewer than <paramref name="count"/> may come
    /// back when the text is short. Used to highlight two keywords for a long observation (both linked
    /// to the same object). With <paramref name="count"/> = 1 this reproduces <see cref="ExtractKeyword"/>.
    ///
    /// <para><paramref name="exclude"/> is what the rest of the phase has already claimed. A block maps
    /// a keyword to ONE object, so the same word surfacing for a second object made that object's
    /// sentence clickable but wired to the first one's — the player clicked the word in the sentence
    /// about the moss and acted on the oak. Excluding the claimed surfaces is what keeps one word one
    /// object; when nothing else is left the object simply gets no keyword, which is the honest
    /// outcome — a word that would act on something else is worse than no word at all.</para>
    /// </summary>
    public static List<string> ExtractKeywords(string text, string referenceLemma, int count, string? ownName = null,
                                               IReadOnlyCollection<string>? exclude = null)
    {
        if (string.IsNullOrWhiteSpace(text) || count <= 0) return new List<string>();

        var taken = exclude is { Count: > 0 }
            ? new HashSet<string>(exclude, StringComparer.OrdinalIgnoreCase)
            : null;
        bool IsTaken(string surface) => taken != null && taken.Contains(surface);

        var candidates = NounExtractor.ExtractNounsWithLemmas(text);
        var refLemma = (referenceLemma ?? string.Empty).ToLowerInvariant().Trim();

        // Playground names the object itself instead. Placeholder prose is the same frame for every
        // object ("My attention is drawn to a X. This is a …"), so the associated-word rule below
        // ranked the frame's own vocabulary — "attention" — for object after object. Every observation
        // in a phase then carried the same keyword, and since a block maps a keyword to the FIRST
        // outcome that claimed it, only the first object observed was ever clickable: a script could
        // not act on the second thing it was told about. Naming the object gives one distinct handle
        // per object, which is the whole point of the mode.
        if (PlaygroundMode.IsActive)
        {
            var own = PlaygroundOwnWord(candidates, ownName, refLemma);
            if (own != null) return IsTaken(own) ? new List<string>() : new List<string> { own };
        }

        // Drop the object's own word (by lemma) so keywords are associated, not the object itself.
        if (candidates.Count > 1 && !string.IsNullOrEmpty(refLemma))
            candidates = candidates.Where(c => !c.Lemma.Equals(refLemma, StringComparison.OrdinalIgnoreCase)).ToList();

        // No nouns at all → longest word fallback (a single keyword only).
        if (candidates.Count == 0)
        {
            var w = LongestWord(text);
            return w != null && !IsTaken(w) ? new List<string> { w } : new List<string>();
        }

        // Rank the candidate surfaces: by embedding similarity to the reference lemma when embeddings
        // are available (closest first — the old single-keyword behaviour for the top pick), otherwise
        // by surface length. The ranked order is what lets us take the two best distinct keywords.
        IEnumerable<string> ranked;
        var refVec = WordEmbedding.IsReady && !string.IsNullOrEmpty(refLemma) ? WordEmbedding.GetVector(refLemma) : null;
        if (refVec != null)
        {
            var scored = candidates
                .Select(c => (c.Surface, Vec: WordEmbedding.GetVector(c.Lemma)))
                .Where(x => x.Vec != null)
                .Select(x => (x.Surface, Sim: WordEmbedding.Cosine(refVec, x.Vec!)))
                .ToList();
            ranked = scored.Count > 0
                ? scored.OrderByDescending(x => x.Sim).Select(x => x.Surface)
                : candidates.OrderByDescending(c => c.Surface.Length).Select(c => c.Surface);
        }
        else
        {
            ranked = candidates.OrderByDescending(c => c.Surface.Length).Select(c => c.Surface);
        }

        // Take the top distinct surfaces (case-insensitive) so a keyword is never highlighted twice.
        var result = new List<string>();
        foreach (var surface in ranked)
        {
            if (IsTaken(surface)) continue;
            if (result.Any(r => r.Equals(surface, StringComparison.OrdinalIgnoreCase))) continue;
            result.Add(surface);
            if (result.Count >= count) break;
        }
        return result;
    }

    /// <summary>
    /// The surface in <paramref name="candidates"/> that IS the object — its own noun — for the
    /// playground keyword rule. Tried in order: the head word of <paramref name="ownName"/> (the last
    /// word of "Courtyard–Pigsty Track", of "a pig"), then <paramref name="refLemma"/>. The name is
    /// tried first because an NPC's reference lemma is the generic "person", which never appears in
    /// the text. Returns null when neither is in the sentence, leaving the normal rule to run.
    /// </summary>
    private static string? PlaygroundOwnWord(
        List<(string Surface, string Lemma)> candidates, string? ownName, string refLemma)
    {
        foreach (var word in new[] { HeadWord(ownName), refLemma })
        {
            if (string.IsNullOrEmpty(word)) continue;
            foreach (var c in candidates)
                if (c.Lemma.Equals(word, StringComparison.OrdinalIgnoreCase)
                 || c.Surface.Equals(word, StringComparison.OrdinalIgnoreCase))
                    return c.Surface;
        }
        return null;
    }

    /// <summary>The last alphabetic word of a display name, lower-cased: "a pig" → "pig".</summary>
    private static string? HeadWord(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var words = Regex.Matches(name, @"[A-Za-z]+");
        return words.Count > 0 ? words[^1].Value.ToLowerInvariant() : null;
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
