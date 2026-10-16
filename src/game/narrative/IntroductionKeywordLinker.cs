using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Links the words of an area introduction to the objects around me, one keyword per object the text
/// names. An introduction is about no object in particular — it says where I am and lists what is
/// there — so the keyword rule for an observation (the word most related to <i>the</i> object) does
/// not apply; this one asks, of each word, which object's <b>name</b> it is.
///
/// <para>The rule, measured against 145 labelled introductions rewritten by the shipped model
/// (<c>tools/keywords/intro/</c>): 2.6 keywords per introduction, 91% on the right object, against
/// one keyword at most, 73% right, for the lemma-or-last-word match it replaced. Asking the model
/// itself which word names what scored 58%, and word-vector similarity 50–73%.</para>
///
/// <list type="number">
/// <item><b>Name words.</b> A word is claimed by an object whose name holds it, as written or in the
/// plural ("racks" for drying racks), or which has it as its reference lemma. The head of the name
/// ("door") outranks a modifier ("pantry", "tenement"). A person answers to their role, their name
/// and their sex ("reeve", "Godric", "woman"), never to the "of the field" that places them — that
/// tail named every acquaintance of the field after the field.</item>
/// <item><b>The area's own words are not the objects'.</b> A word of the "I find myself …" clause
/// names where I am: on the tar street, "tar" is not the horse market–tar street way. This one rule
/// removed most wrong links.</item>
/// <item><b>A connector answers only to its kind of way</b> (track, path, crossing…), never to the
/// two places it joins, which the text uses for those places.</item>
/// <item><b>A place phrase is not a thing.</b> In "a wooden chest by the door", "door" says where the
/// chest is; a word whose first occurrence sits inside an object's parenthesised place
/// ("(by the door)") is not taken.</item>
/// <item><b>A shared word goes to its neighbours' object.</b> When several objects claim a word, the
/// one whose other name words stand around its first occurrence takes it; a tie links nothing.</item>
/// </list>
///
/// <para><b>Only the first occurrence counts</b>, because that is the one the scroll buffer
/// highlights: in "the tenement door … the ivy-grown house door", "door" can only ever be the
/// tenement door, and the second door has to be reached by "ivy-grown" or "house".</para>
/// </summary>
public static class IntroductionKeywordLinker
{
    private static readonly HashSet<string> Stop = new(StringComparer.OrdinalIgnoreCase)
        { "a", "an", "the", "of", "some", "my", "and", "in", "on", "by", "at", "to", "with", "from" };

    private static readonly Regex Word = new(@"[A-Za-z][A-Za-z'-]*", RegexOptions.Compiled);

    /// <summary>
    /// The keyword of each object of <paramref name="pool"/> that <paramref name="text"/> names, in
    /// reading order. <paramref name="candidates"/> are the text's keyword candidates
    /// (<see cref="NounExtractor.ExtractKeywordCandidates"/>); <paramref name="whereClause"/> is the
    /// area's context description ("on the tar street"), whose words name the area itself.
    /// </summary>
    public static List<(string Keyword, NarrativeAnchor Anchor)> Link(
        string text, string whereClause, IReadOnlyList<NarrativeAnchor> pool,
        IReadOnlyList<(string Surface, string Lemma)> candidates)
    {
        var areaWords = new HashSet<string>(Words(whereClause).Where(w => !Stop.Contains(w)), StringComparer.OrdinalIgnoreCase);
        var objects = pool.OfType<ObservationObject>().Select(o => new Entry(o, areaWords)).ToList();
        var placeSpans = PlaceSpans(text, objects);

        // word -> the objects claiming it, each with the rank of the claim (0 = head or lemma)
        var claims = new Dictionary<string, List<(Entry Obj, int Rank)>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (surface, _) in candidates)
        {
            int pos = FirstPosition(text, surface);
            if (pos < 0 || placeSpans.Any(s => pos >= s.Start && pos < s.End)) continue;
            foreach (var obj in objects)
            {
                int rank = obj.Claim(surface);
                if (rank < 0) continue;
                if (!claims.TryGetValue(surface, out var list)) claims[surface] = list = new();
                list.Add((obj, rank));
            }
        }

        var best = new Dictionary<Entry, (int Rank, int Pos, string Word)>();
        foreach (var (word, list) in claims)
        {
            var claim = list[0];
            if (list.Select(c => c.Obj).Distinct().Count() > 1)
            {
                var owner = ContextOwner(text, word, list.Select(c => c.Obj).Distinct().ToList());
                if (owner == null) continue;
                claim = list.First(c => c.Obj == owner);
            }
            var cand = (claim.Rank, FirstPosition(text, word), word);
            if (!best.TryGetValue(claim.Obj, out var held) || (cand.Rank, cand.Item2).CompareTo((held.Rank, held.Pos)) < 0)
                best[claim.Obj] = cand;
        }

        return best.OrderBy(kv => kv.Value.Pos)
                   .Select(kv => (kv.Value.Word, (NarrativeAnchor)kv.Key.Object))
                   .ToList();
    }

    /// <summary>
    /// Which of <paramref name="rivals"/> the first occurrence of <paramref name="word"/> belongs to:
    /// the one with most of its name or place words among the three words before it and the one after.
    /// Null on a tie or when none has any.
    /// </summary>
    private static Entry? ContextOwner(string text, string word, List<Entry> rivals)
    {
        var tokens = Word.Matches(text).Select(m => (m.Index, Value: m.Value.ToLowerInvariant())).ToList();
        int pos = FirstPosition(text, word);
        int at = tokens.FindIndex(t => t.Index == pos);
        if (at < 0) return null;
        var window = tokens.Skip(Math.Max(0, at - 3)).Take(at - Math.Max(0, at - 3) + 2).Select(t => t.Value).ToList();

        var scored = rivals.Select(r => (r, Score: r.ContextWords.Count(w => window.Any(t => SurfaceIs(t, w))))).ToList();
        int top = scored.Max(s => s.Score);
        var leaders = scored.Where(s => s.Score == top).ToList();
        return top > 0 && leaders.Count == 1 ? leaders[0].r : null;
    }

    /// <summary>Spans of the text that repeat an object's place phrase ("by the door").</summary>
    private static List<(int Start, int End)> PlaceSpans(string text, List<Entry> objects)
    {
        var spans = new List<(int, int)>();
        foreach (var phrase in objects.Select(o => o.Place).Where(p => p.Contains(' ')).Distinct())
            foreach (Match m in Regex.Matches(text, @"\b" + Regex.Escape(phrase) + @"\b", RegexOptions.IgnoreCase))
                spans.Add((m.Index, m.Index + m.Length));
        return spans;
    }

    /// <summary>Where the scroll buffer will highlight <paramref name="word"/>: its first whole-word occurrence.</summary>
    private static int FirstPosition(string text, string word)
    {
        var m = Regex.Match(text, @"\b" + Regex.Escape(word) + @"\b", RegexOptions.IgnoreCase);
        return m.Success ? m.Index : -1;
    }

    /// <summary>The word as written, or its plural. The tagger's lemma is not trusted here: it makes
    /// "crossing" a cross.</summary>
    private static bool SurfaceIs(string surface, string word)
    {
        surface = surface.ToLowerInvariant(); word = word.ToLowerInvariant();
        return surface == word || surface == word + "s" || surface == word + "es"
            || (word.EndsWith("y") && surface == word[..^1] + "ies");
    }

    private static bool InPlural(string word, HashSet<string> set)
        => set.Contains(word) || set.Contains(word + "s") || set.Contains(word + "es") || set.Contains(word.TrimEnd('s'));

    private static IEnumerable<string> Words(string? s)
        => s == null ? Enumerable.Empty<string>() : Word.Matches(s).Select(m => m.Value.ToLowerInvariant());

    /// <summary>One object's claimable words.</summary>
    private sealed class Entry
    {
        public ObservationObject Object { get; }
        /// <summary>Its name and place words, to tell it from a rival sharing a word.</summary>
        public List<string> ContextWords { get; }
        /// <summary>The parenthesised place of a thing ("by the door"), or empty.</summary>
        public string Place { get; }
        /// <summary>Words that name it outright: the head of its name, its lemma, a person's role and own name.</summary>
        private readonly HashSet<string> _primary = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>Words that only qualify it: the rest of its name ("pantry", "tenement"), a person's sex.</summary>
        private readonly HashSet<string> _secondary = new(StringComparer.OrdinalIgnoreCase);

        public Entry(ObservationObject obj, HashSet<string> areaWords)
        {
            Object = obj;
            string name = obj.NeutralName;
            var paren = Regex.Match(name, @"\(([^)]*)\)");
            string bare = Regex.Replace(name, @"\(.*?\)", " ").Replace('–', ' ');

            List<string> nameWords;
            if (obj is SyntheticNpcObservationObject npc)
            {
                // "the reeve of the field (an old man)", or "my friend Godric, the reeve of the field":
                // the role before "of", the person's own name, and the sex from the parenthesis.
                int comma = bare.LastIndexOf(", ", StringComparison.Ordinal);
                string role = Regex.Split(comma >= 0 ? bare[(comma + 2)..] : bare, @"\bof the\b")[0];
                var roleWords = Words(role).Where(w => !Stop.Contains(w) && w is not ("young" or "old")).Reverse().ToList();
                var ownName = Words(npc.NpcEntity?.DisplayName)
                    .Concat(comma >= 0 ? Words(bare[..comma]).Where(w => !Stop.Contains(w)) : Enumerable.Empty<string>())
                    .ToList();
                var sex = Words(paren.Success ? paren.Groups[1].Value : "").Where(w => w is "man" or "woman" or "boy" or "girl");
                Add(_primary, roleWords.Take(1).Concat(ownName), areaWords);
                Add(_secondary, roleWords.Skip(1).Concat(sex), areaWords);
                nameWords = roleWords.Concat(ownName).ToList();
                Place = "";
            }
            else
            {
                var words = Words(bare).Where(w => !Stop.Contains(w)).Reverse().ToList();   // head first
                bool connector = name.Contains('–');                                        // a connector is its kind of way
                Add(_primary, words.Take(1), areaWords);
                if (!connector) Add(_secondary, words.Skip(1), areaWords);
                string lemma = obj.ReferenceLemma.Trim().ToLowerInvariant();
                if (lemma.Length > 0) Add(_primary, new[] { lemma }, areaWords);
                nameWords = words;
                Place = paren.Success ? paren.Groups[1].Value.ToLowerInvariant() : "";
            }
            ContextWords = nameWords.Concat(Words(Place).Where(w => !Stop.Contains(w))).Distinct().ToList();
            _secondary.ExceptWith(_primary);
        }

        /// <summary>Adds the words that do not name the area I am in.</summary>
        private static void Add(HashSet<string> set, IEnumerable<string> words, HashSet<string> areaWords)
        {
            foreach (var w in words)
                if (!InPlural(w, areaWords)) set.Add(w);
        }

        /// <summary>The rank of this object's claim on <paramref name="surface"/>: 0 when the word names
        /// it outright, 1 when it only qualifies it, -1 for no claim.</summary>
        public int Claim(string surface)
        {
            if (_primary.Any(w => SurfaceIs(surface, w))) return 0;
            if (_secondary.Any(w => SurfaceIs(surface, w))) return 1;
            return -1;
        }
    }
}
