using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Picks the clickable keyword for an observation sentence by rule, not by LLM: extract the words a
/// keyword may be drawn from (nouns and adjectives, with lemmas) from the final sanitized text, then
/// rank them by relatedness to the observation object's reference lemma. Falls back to the longest
/// candidate (or the longest word) when embeddings or candidates are unavailable.
///
/// <para><b>Everything close enough to the object is an equally good handle on it</b>
/// (<see cref="Config.Narrative.KeywordSamplingThreshold"/>), so the keyword is drawn uniformly from
/// that pool rather than always being the single best word — which is what keeps the same object
/// from being highlighted by the same word every time. Below the threshold the ranking stands, so an
/// object with nothing else about it in the sentence keeps its best word deterministically.</para>
///
/// <para><b>The object's own word is always in the pool</b>, whatever it scored. The score measures
/// similarity to the anchor, a proxy for "is this word about the object"; the own word needs no
/// proxy, being the object's name sitting in the prose. Excluding it — which this rule once did on
/// purpose — left the one unambiguous handle as the one word that could never be chosen.</para>
///
/// <para><b>Ranking subtracts each candidate's centrality</b> (<see cref="WordEmbedding.Centrality"/>)
/// rather than taking raw cosine, because cosine alone ranks partly by word frequency. See that
/// method for the measurements; the short version is that "time" is closer to everything than 99% of
/// English is, so it beat the specific word in any sentence whose object was not itself unusual —
/// <c>wolf</c> ranked <c>time</c> above <c>fur</c> and <c>roof</c> ranked it above <c>village</c>.</para>
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
    /// Returns up to <paramref name="count"/> distinct keywords (surfaces from the text) for the
    /// object described by <paramref name="referenceLemma"/> and <paramref name="ownName"/>. Distinct
    /// by case-insensitive surface, so the same word is never returned twice within one sentence;
    /// fewer than <paramref name="count"/> may come back when the text is short. Used to highlight
    /// two keywords for a long observation (both linked to the same object). With
    /// <paramref name="count"/> = 1 this reproduces <see cref="ExtractKeyword"/>.
    ///
    /// <para>Nothing is withheld on account of what another object of the same block chose. Two
    /// sentences about two men both offer "man", each wired to its own man, because the anchor rides
    /// on the sentence rather than on a per-block table keyed by the word.</para>
    /// </summary>
    public static List<string> ExtractKeywords(string text, string referenceLemma, int count, string? ownName = null)
    {
        if (string.IsNullOrWhiteSpace(text) || count <= 0) return new List<string>();

        var candidates = NounExtractor.ExtractKeywordCandidates(text);
        var refLemma = (referenceLemma ?? string.Empty).ToLowerInvariant().Trim();

        // No candidate words at all → longest word fallback (a single keyword only).
        if (candidates.Count == 0)
        {
            var w = LongestWord(text);
            return w != null ? new List<string> { w } : new List<string>();
        }

        var refVec = WordEmbedding.IsReady && !string.IsNullOrEmpty(refLemma) ? WordEmbedding.GetVector(refLemma) : null;
        var own = OwnWord(candidates, ownName, refLemma);
        IEnumerable<string> ranked;

        if (refVec != null)
        {
            // Relatedness is the similarity to the reference lemma MINUS the candidate's own
            // centrality, because a raw cosine ranks a frequent, general word highly against any
            // anchor at all — "time" sits closer to everything than 99% of English does.
            var scored = candidates
                .Select(c => (c.Surface, c.Lemma, Vec: WordEmbedding.GetVector(c.Lemma)))
                .Where(x => x.Vec != null)
                .Select(x => (x.Surface, Score: WordEmbedding.Cosine(refVec, x.Vec!) - WordEmbedding.Centrality(x.Lemma)))
                .OrderByDescending(x => x.Score)
                .ToList();

            // Everything at or above the threshold is an equally good handle on the object, so the
            // choice between them is a draw rather than a ranking — which is what stops the same
            // word being highlighted for the same object every single time.
            var chosen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pool = new List<string>();
            foreach (var x in scored)
                if (x.Score >= Config.Narrative.KeywordSamplingThreshold && chosen.Add(x.Surface))
                    pool.Add(x.Surface);

            // The object's own word joins the pool whatever it scored. The score measures similarity
            // to the ANCHOR, which is a proxy for "is this word about the object" — and the own word
            // needs no proxy, being the object's name sitting in the prose. For scenery this changes
            // nothing (its lemma IS the reference lemma, so it scores ~0.9 and was already in). It
            // matters where the anchor is a stand-in: against "body", "woman" scores 0.42 and stays,
            // but "stranger" 0.15, "reeve" 0.15 and "brewer" 0.01 would all be thrown out, leaving a
            // person clickable as "coat" or "hands" but never by a word that names them.
            if (own != null && chosen.Add(own)) pool.Add(own);

            Shuffle(pool);

            // Below the threshold, ranked order stands. An empty pool therefore leaves the
            // best-scoring word first, which is the fallback when nothing is close enough.
            ranked = pool.Concat(scored.Select(x => x.Surface).Where(s => !chosen.Contains(s)));
        }
        else
        {
            // Nothing can be scored — the ordinary state of a --playground run, where the vectors
            // are still loading. Ranking degrades to surface length, so lead with the own word to
            // keep one distinct, meaningful handle per object; without it every object in the
            // placeholder frame comes out as "attention", its longest word.
            var byLength = candidates.OrderByDescending(c => c.Surface.Length).Select(c => c.Surface);
            ranked = own != null ? new[] { own }.Concat(byLength) : byLength;
        }

        // Take the top distinct surfaces (case-insensitive) so one sentence never highlights the
        // same word twice. Sentences do not constrain each other — see the summary above.
        var result = new List<string>();
        foreach (var surface in ranked)
        {
            if (result.Any(r => r.Equals(surface, StringComparison.OrdinalIgnoreCase))) continue;
            result.Add(surface);
            if (result.Count >= count) break;
        }
        return result;
    }

    /// <summary>
    /// Fisher-Yates over <paramref name="items"/>. Drawn from the master-seeded <c>keyword</c>
    /// stream so a <c>--seed</c> run still highlights the same words in the same order; locked
    /// because narration is generated on background threads and a shared <see cref="Random"/> is
    /// not thread-safe. Nothing else draws from that stream, so this lock covers it.
    /// </summary>
    private static void Shuffle(List<string> items)
    {
        if (items.Count < 2) return;
        var rng = GameRng.Stream("keyword");
        lock (_rngLock)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }
    }

    private static readonly object _rngLock = new();

    /// <summary>
    /// The surface in <paramref name="candidates"/> that IS the object — its own noun. Tried in
    /// order: the head word of <paramref name="ownName"/> (the last word of "Courtyard–Pigsty Track",
    /// of "a pig"), then <paramref name="refLemma"/>. The name is tried first because an NPC's
    /// reference lemma is the generic "person", which never appears in the text. Returns null when
    /// neither is in the sentence, leaving the association ranking to choose alone.
    ///
    /// <para>This is also what makes <c>--playground</c> work. Placeholder prose is one frame reused
    /// for every object ("My attention is drawn to a X. This is a …"), and the vectors are usually
    /// still loading when a script reaches its first observation — so ranking by length returns the
    /// frame's own longest word, "attention", for object after object. Leading with the object's own
    /// word gives one distinct handle per object, which is what makes a scripted phase clickable.</para>
    /// </summary>
    private static string? OwnWord(
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
