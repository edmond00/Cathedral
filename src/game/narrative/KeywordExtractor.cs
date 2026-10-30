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
/// <para><b>A word that names the object wins.</b> For a person that is a word of what they are
/// called — their name once known, their role, their sex ("the reaper of the radish field (a man)" is
/// named by <c>reaper</c> or <c>man</c>), drawn uniformly when the prose carries several. For a thing
/// it is the head word of its name or its reference lemma, the first such word in the text. Only
/// when the prose names the object nowhere does the association ranking choose, and then its single
/// best word, never a draw.</para>
///
/// <para>Measured on 342 labelled real rewrites (<c>tools/keywords/bench_hybrids.py</c>, the H2
/// rule): the keyword names the object 95% of the time, against 51% for the rule it replaced, which
/// drew uniformly from every candidate scoring 0.25 above the anchor. That draw is what highlighted <c>case</c> in "just in case" for a
/// reaper while <c>figure</c> sat in the same sentence: a quarter of the time the pool's other words
/// were features, or nothing to do with the object at all.</para>
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
    ///
    /// <para><paramref name="flavourLemmas"/> is the voice the text was rewritten into
    /// (<see cref="Sanitizer.PersonaFlavour.KeywordLemmas"/>): a word it wrote in place of a noun is
    /// read as that noun, so "steed" names the horse it replaced and becomes its keyword.</para>
    /// </summary>
    public static List<string> ExtractKeywords(string text, string referenceLemma, int count, string? ownName = null,
                                               bool isPerson = false,
                                               IReadOnlyDictionary<string, string>? flavourLemmas = null)
    {
        if (string.IsNullOrWhiteSpace(text) || count <= 0) return new List<string>();

        var candidates = NounExtractor.ExtractKeywordCandidates(text);
        if (flavourLemmas != null)
            candidates = candidates
                .Select(c => flavourLemmas.TryGetValue(c.Surface.ToLowerInvariant(), out var lemma) ? (c.Surface, lemma) : c)
                .ToList();
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

            // The words that name the object lead; the association ranking follows, so a second
            // keyword (a long observation) and an object the prose never names both fall to the best
            // remaining word rather than to a draw. A person's anchor is a stand-in ("body"), against
            // which "brewer" scores 0.01 — the ranking alone would make them clickable as "coat".
            List<string> naming;
            if (isPerson)
            {
                var names = PersonNameWords(ownName);
                naming = candidates.Where(c => LemIn(c, names))
                                   .Select(c => c.Surface)
                                   .Distinct(StringComparer.OrdinalIgnoreCase)
                                   .ToList();
                Shuffle(naming);
            }
            else
            {
                var heads = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (HeadWord(StripParenthesis(ownName)) is { } head) heads.Add(head);
                if (refLemma.Length > 0) heads.Add(refLemma);
                naming = candidates.Where(c => LemIn(c, heads)).Select(c => c.Surface).Take(1).ToList();
            }

            ranked = naming.Concat(scored.Select(x => x.Surface));
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

    /// <summary>
    /// The words that name a person, from their neutral name: an acquaintance's proper name and role
    /// ("my acquaintance Hugh, the reeve of the field" → hugh, reeve), else the role and the sex,
    /// never the "of the field" tail ("the reaper of the radish field (a man)" → reaper, man).
    /// </summary>
    private static HashSet<string> PersonNameWords(string? name)
    {
        var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(name)) return words;

        // The name may be several words: the real one ("Daphnaaelh Eghbert") is what the text carries
        // by the time keywords are chosen, not the one-word false name the LLM was shown.
        var acquaintance = Regex.Match(name, @"my acquaintance (.+?), the (.+?) of the", RegexOptions.IgnoreCase);
        if (acquaintance.Success)
        {
            foreach (var w in acquaintance.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                words.Add(w);
            foreach (var w in acquaintance.Groups[2].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                words.Add(w);
            return words;
        }

        var n = Regex.Replace(name, @"\bof the [^(]*", "", RegexOptions.IgnoreCase).Replace("young ", "");
        foreach (Match m in Regex.Matches(n, @"[A-Za-z]+"))
            if (m.Value.Length > 1 && !m.Value.Equals("and", StringComparison.OrdinalIgnoreCase)
                && !m.Value.Equals("the", StringComparison.OrdinalIgnoreCase))
                words.Add(m.Value);
        words.Remove("a");
        return words;
    }

    /// <summary>A candidate names one of <paramref name="words"/>: its lemma or surface, singular or plural.</summary>
    private static bool LemIn((string Surface, string Lemma) c, HashSet<string> words)
    {
        var s = c.Surface.ToLowerInvariant();
        return words.Contains(c.Lemma) || words.Contains(s) || words.Contains(s.TrimEnd('s')) || words.Contains(s + "s");
    }

    private static string? StripParenthesis(string? name)
        => name == null ? null : Regex.Replace(name, @"\(.*?\)", "");

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
