using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Cathedral.Game.Narrative.Sanitizer;

/// <summary>
/// Layer 1 of the text sanitization pipeline: direct word replacement, with no request made.
/// Every word or phrase in <see cref="ForbiddenWords"/> is replaced in place by a word of the world —
/// "a mile" → "a league", "last week" → "last eleven-day", "the Moon" → "the eggmoon".
///
/// <para>
/// <b>Every surface form is listed, never stemmed.</b> The stemmer this replaced reduced a token to a
/// key and pasted the key's replacement over whatever form it had found, so "computers" became
/// "arcane device", "he experimented" became "he trial", and "OK" became "VERY WELL". The list now
/// pairs each form with the matching form of its replacement (plural with plural, past with past), and
/// the helpers in <see cref="ForbiddenWords"/> generate the regular ones so they cannot be forgotten.
/// </para>
/// <para>
/// Three things keep the sentence grammatical around a replacement: the casing follows the original
/// and the position (a proper noun replaced mid-sentence keeps the replacement's own case; any word at
/// the start of a sentence is capitalised); an indefinite article before it is re-agreed ("a mile" →
/// "a league", "a week" → "an eleven-day"); and a replacement that carries its own article drops it
/// when the text already had one ("the Earth" → "the world", not "the the world").
/// </para>
/// </summary>
public static class ForbiddenWordsDictionary
{
    /// <summary>
    /// One replaceable surface form. <paramref name="Counted"/>, when set, rewrites a count together
    /// with its noun — "six centuries" → "six hundred rounds", "two months" → "sixty days" — and is
    /// handed the count as written, a number (digits or words) or a quantifier ("a few", "several").
    /// </summary>
    public sealed record Entry(string Form, string Replacement, bool Proper, bool MidSentenceOnly,
                               Func<Quantity, string>? Counted = null)
    {
        /// <summary>The section of <see cref="ForbiddenWords"/> it was listed under, for the dump.</summary>
        public string Category { get; init; } = "";
    }

    /// <summary>
    /// A count before a noun: its value when it is a number, and its words as written.
    /// <paramref name="Per"/> marks a rate ("twice a month", "a tenth a month"), where "a" means "in each".
    /// </summary>
    public readonly record struct Quantity(string Text, long? Value, bool Per = false);

    // Exact-case forms (proper nouns, and the words whose meaning depends on case: "Mercury" the god,
    // "May" the month), then case-insensitive forms keyed in lower case.
    private static readonly Dictionary<string, Entry> _exact;
    private static readonly Dictionary<string, Entry> _folded;
    // One small pattern per first word ("full" → "full moon"), tried only where that word begins.
    private static readonly Dictionary<string, Regex> _byFirstWord;

    /// <summary>Every form the dictionary replaces, for audits and tests.</summary>
    public static IReadOnlyCollection<Entry> Entries { get; }

    static ForbiddenWordsDictionary()
    {
        _exact  = new Dictionary<string, Entry>(StringComparer.Ordinal);
        _folded = new Dictionary<string, Entry>(StringComparer.Ordinal);

        foreach (var e in ForbiddenWords.Build())
        {
            string key = Normalise(e.Form);
            if (e.Proper) _exact[key] = e;
            else          _folded[key.ToLowerInvariant()] = e;
        }
        Entries = _exact.Values.Concat(_folded.Values).ToList();

        // Longest first, so "outer space" wins over "space" and "Alexander the Great" over nothing.
        var forms = Entries.Select(e => e.Form).Distinct(StringComparer.OrdinalIgnoreCase)
                           .OrderByDescending(f => f.Length).ToList();

        // Anchored at a word start, and not followed by a letter or a hyphen: "sun-baked" and
        // "gun-metal" are left alone rather than turned into "Eye of Day-baked". A hyphen before is
        // allowed, since the word is then the head of the compound: "ten-year-old", "half-moon".
        // One pattern of every form tried at every character cost milliseconds a sentence, and the
        // preview re-applies this on every word the model streams; grouped by first word, a word
        // that begins no form costs one hash lookup.
        _byFirstWord = forms
            .GroupBy(f => new string(f.TakeWhile(char.IsLetterOrDigit).ToArray()), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => new Regex(@"\G(?:" + string.Join("|", g.Select(FormPattern)) + @")(?![\p{L}\p{N}_\-])",
                               RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
                StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<Match> Matches(string text)
    {
        int i = 0;
        while (i < text.Length)
        {
            if (!char.IsLetterOrDigit(text[i])) { i++; continue; }
            int start = i;
            while (i < text.Length && char.IsLetterOrDigit(text[i])) i++;
            bool wordStart = start == 0 || !(char.IsLetterOrDigit(text[start - 1]) || text[start - 1] is '_' or '\'' or '’');
            if (!wordStart || !_byFirstWord.TryGetValue(text[start..i], out var pattern)) continue;

            var m = pattern.Match(text, start);
            if (!m.Success) continue;
            yield return m;
            i = Math.Max(i, m.Index + m.Length);
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Replaces every forbidden form in <paramref name="text"/>. A possessive is kept ("the computer's
    /// lid" → "the reckoning engine's lid"), since the apostrophe is left outside the match.
    /// <para>
    /// <paramref name="sourceVocabulary"/> (see <see cref="SourceVocabulary"/>) spares a name the game
    /// itself handed the model: a form capitalised in mid-sentence and made only of words the request
    /// supplied is part of a generated name — an order called the Ashen Star, a family called North
    /// March — and renaming it would make the narration disagree with every screen that shows it.
    /// The same word in lower case is still replaced, so a common word the game's own text let slip
    /// gets no licence from this.
    /// </para>
    /// </summary>
    public static string Apply(string text, IReadOnlySet<string>? sourceVocabulary = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var sb = new StringBuilder(text.Length + 16);
        int last = 0;

        foreach (Match m in Matches(text))
        {
            string original = m.Value;
            var entry = Lookup(original);
            if (entry == null) continue;

            sb.Append(text, last, m.Index - last);
            last = m.Index + m.Length;

            bool sentenceStart = IsSentenceStart(sb);
            bool suppliedName = !sentenceStart && char.IsUpper(original[0])
                                && SourceVocabulary.Contains(sourceVocabulary, original);
            if ((entry.MidSentenceOnly && sentenceStart) || suppliedName)
            {
                sb.Append(original);
                continue;
            }

            // A counted unit takes its count with it: "six centuries" → "six hundred rounds".
            if (entry.Counted != null && TrailingQuantity(sb) is { } q)
            {
                bool capital = char.IsUpper(q.Quantity.Text[0]);
                sb.Remove(q.Index, sb.Length - q.Index);
                string counted = entry.Counted(q.Quantity with { Text = q.Quantity.Text.ToLowerInvariant() });
                sb.Append(capital ? Capitalise(counted) : counted);
                last = KeepPossessive(text, last, original, counted, sb);
                continue;
            }

            string replacement = entry.Replacement;

            // A replacement carrying its own article yields to one already in the text.
            var article = TrailingArticle(sb);
            if (article != null && LeadingArticleLength(replacement) is int n and > 0)
                replacement = replacement[n..];

            replacement = Case(replacement, original, entry, sentenceStart && article == null);

            if (article is { } a && (a.Word.Equals("a", StringComparison.OrdinalIgnoreCase)
                                     || a.Word.Equals("an", StringComparison.OrdinalIgnoreCase)))
            {
                string agreed = StartsWithVowelSound(replacement) ? "an" : "a";
                agreed = MatchCase(agreed, a.Word);
                sb.Remove(a.Index, a.Word.Length).Insert(a.Index, agreed);
            }

            sb.Append(replacement);
            last = KeepPossessive(text, last, original, replacement, sb);
        }

        if (last == 0) return text;
        sb.Append(text, last, text.Length - last);
        return sb.ToString();
    }

    /// <summary>The forms of <paramref name="text"/> that <see cref="Apply"/> would replace.</summary>
    public static IEnumerable<string> FindForms(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;
        foreach (Match m in Matches(text))
            if (Lookup(m.Value) is { } e && e.Replacement != e.Form)
                yield return m.Value;
    }

    // ── Matching ───────────────────────────────────────────────────────────────

    private static Entry? Lookup(string original)
    {
        string key = Normalise(original);
        if (_exact.TryGetValue(key, out var e)) return e;
        if (_folded.TryGetValue(key.ToLowerInvariant(), out e)) return e;
        return null;
    }

    // Whitespace runs collapse to one space and the typographic apostrophe to the plain one, so a
    // phrase broken over a line or written "o’clock" still finds its entry.
    private static string Normalise(string s)
        => Regex.Replace(s, @"\s+", " ").Replace('’', '\'');

    private static string FormPattern(string form)
    {
        var sb = new StringBuilder();
        foreach (char c in form)
        {
            if (char.IsWhiteSpace(c)) sb.Append(@"\s+");
            else if (c == '\'' || c == '’') sb.Append("['’]");
            else sb.Append(Regex.Escape(c.ToString()));
        }
        return sb.ToString();
    }

    // ── Grammar around the replacement ─────────────────────────────────────────

    private static bool IsSentenceStart(StringBuilder sb)
    {
        for (int i = sb.Length - 1; i >= 0; i--)
        {
            char c = sb[i];
            if (char.IsWhiteSpace(c)) { if (c == '\n') return true; continue; }
            if (c is '"' or '“' or '«' or '(' or '[') continue;  // an opening quote starts an utterance
            return c is '.' or '!' or '?' or '…' or ':';
        }
        return true;
    }

    private readonly record struct Article(int Index, string Word);

    // Any determiner takes the place of a replacement's own article ("one August" → "one Drought",
    // not "one the Drought"); only "a" and "an" are re-agreed with what follows.
    private static readonly Regex _trailingArticle =
        new(@"(?<![\p{L}'’])(a|an|the|one|this|that|last|next|every|each|some|any|no|his|her|its|their|our|my|your)\s+$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static Article? TrailingArticle(StringBuilder sb)
    {
        // Only the tail can matter; looking at a few characters keeps this linear in the text.
        int start = Math.Max(0, sb.Length - 16);
        string tail = sb.ToString(start, sb.Length - start);
        var m = _trailingArticle.Match(tail);
        if (!m.Success) return null;
        // "a" must be a word of its own, not the end of a longer one cut by the window.
        int idx = start + m.Groups[1].Index;
        if (idx > 0 && char.IsLetter(sb[idx - 1])) return null;
        return new Article(idx, m.Groups[1].Value);
    }

    // "the month's end" → "the season's end", but "a month's wages" → "thirty days' wages": a
    // replacement that came out plural takes the plural possessive.
    private static int KeepPossessive(string text, int at, string original, string replacement, StringBuilder sb)
    {
        if (at + 1 >= text.Length || text[at] is not ('\'' or '’') || text[at + 1] != 's') return at;
        if (at + 2 < text.Length && char.IsLetter(text[at + 2])) return at;
        if (!replacement.EndsWith('s') || original.EndsWith('s') || original.EndsWith('S')) return at;
        sb.Append(text[at]);
        return at + 2;
    }

    private readonly record struct CountAt(int Index, Quantity Quantity);

    // The word before "a" that makes it a rate rather than a count: "twice a month", "three a month",
    // "a tenth a month".
    private static readonly Regex _rateBefore = new(
        @"(?<![\p{L}])(once|twice|thrice|times|\d+|\p{L}+th|"
        + string.Join("|", _unitsForRate()) + @")\s+$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static IEnumerable<string> _unitsForRate()
        => new[] { "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "few", "many" };

    private static readonly string[] _units =
        { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven",
          "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
    private static readonly string[] _tens =
        { "", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };

    private static readonly Regex _trailingCount = new(
        @"(?<![\p{L}\p{N}'’])(\d{1,6}|a\s+few|a\s+couple\s+of|few|several|many|a|an|"
        + string.Join("|", _units.Skip(1).Concat(_tens.Skip(2)).Concat(new[] { "a hundred", "a thousand" }))
        + @")\s+$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static CountAt? TrailingQuantity(StringBuilder sb)
    {
        int start = Math.Max(0, sb.Length - 24);
        string tail = sb.ToString(start, sb.Length - start);
        var m = _trailingCount.Match(tail);
        if (!m.Success) return null;
        int idx = start + m.Groups[1].Index;
        if (idx > 0 && char.IsLetterOrDigit(sb[idx - 1])) return null;
        string text = Regex.Replace(m.Groups[1].Value, @"\s+", " ");
        bool per = text.Equals("a", StringComparison.OrdinalIgnoreCase)
                   && _rateBefore.IsMatch(sb.ToString(Math.Max(0, idx - 16), idx - Math.Max(0, idx - 16)));
        return new CountAt(idx, new Quantity(text, ValueOf(text), per));
    }

    private static long? ValueOf(string text)
    {
        string t = text.ToLowerInvariant();
        if (long.TryParse(t, out long n)) return n;
        if (t is "a" or "an") return 1;
        if (t == "a hundred") return 100;
        if (t == "a thousand") return 1000;
        int u = Array.IndexOf(_units, t);
        if (u > 0) return u;
        int d = Array.IndexOf(_tens, t);
        if (d >= 2) return d * 10;
        return null;   // "a few", "several", "many"
    }

    /// <summary>
    /// A number as the count before it was written: words stay words ("six" → "sixty"), digits stay
    /// digits ("6" → "60").
    /// </summary>
    internal static string Number(long n, Quantity like)
        => like.Text.Length > 0 && char.IsDigit(like.Text[0]) ? n.ToString() : NumberWords(n);

    internal static string NumberWords(long n)
    {
        if (n < 20) return _units[n];
        if (n < 100) return _tens[n / 10] + (n % 10 == 0 ? "" : "-" + _units[n % 10]);
        if (n < 1000) return _units[n / 100] + " hundred" + (n % 100 == 0 ? "" : " and " + NumberWords(n % 100));
        if (n < 1_000_000) return NumberWords(n / 1000) + " thousand" + (n % 1000 == 0 ? "" : (n % 1000 < 100 ? " and " : " ") + NumberWords(n % 1000));
        return n.ToString();
    }

    private static int? LeadingArticleLength(string replacement)
    {
        foreach (var a in new[] { "the ", "a ", "an " })
            if (replacement.StartsWith(a, StringComparison.OrdinalIgnoreCase))
                return a.Length;
        return null;
    }

    private static readonly string[] _consonantSoundPrefixes = { "uni", "use", "usu", "uti", "eu", "ewe", "one", "once" };
    private static readonly string[] _vowelSoundPrefixes     = { "hour", "honest", "honour", "honor", "heir" };

    internal static bool StartsWithVowelSound(string word)
    {
        string w = word.TrimStart().ToLowerInvariant();
        if (w.Length == 0) return false;
        if (_vowelSoundPrefixes.Any(w.StartsWith)) return true;
        if (_consonantSoundPrefixes.Any(w.StartsWith)) return false;
        return "aeiou".IndexOf(w[0]) >= 0;
    }

    private static string Case(string replacement, string original, Entry entry, bool sentenceStart)
    {
        if (replacement.Length == 0) return replacement;
        bool proper = entry.Proper;

        // Shouted ("OKAY!") is carried over; an acronym ("TV", "DNA") is merely written that way.
        bool allCaps = original.Length > 1 && original.Any(char.IsLetter)
                       && original == original.ToUpperInvariant();
        bool acronym = entry.Form == entry.Form.ToUpperInvariant();
        if (allCaps && !proper && !acronym) return replacement.ToUpperInvariant();

        if (sentenceStart) return Capitalise(replacement);
        if (allCaps && acronym) return replacement;

        // A proper noun is capitalised because it is one; its replacement keeps its own case
        // ("the Roman road" → "the imperial road"). A common word capitalised mid-sentence was
        // capitalised for emphasis or as a title, so the emphasis is carried over.
        if (proper) return replacement;
        return char.IsUpper(original[0]) ? Capitalise(replacement) : replacement;
    }

    private static string Capitalise(string s) => char.ToUpperInvariant(s[0]) + s[1..];

    private static string MatchCase(string word, string like)
    {
        if (like.Length > 1 && like == like.ToUpperInvariant()) return word.ToUpperInvariant();
        return char.IsUpper(like[0]) ? Capitalise(word) : word;
    }
}
