using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative.Sanitizer;

/// <summary>
/// The helpers a word list is written with: <see cref="N"/> adds a noun and its plural,
/// <see cref="V"/> takes a verb's forms side by side, <see cref="P"/> a proper noun, and so on, so
/// that every surface form is listed and none is forgotten. A list is a subclass that names its
/// <see cref="Sections"/>; <see cref="Build"/> runs them and stamps each entry with its section.
/// </summary>
public abstract class LexiconBuilder
{
    /// <summary>The entries listed so far.</summary>
    protected List<Lexicon.Entry> List { get; } = new();

    /// <summary>The list's sections, in order: each adds its entries through the helpers.</summary>
    protected abstract IEnumerable<(string Category, Action Section)> Sections();

    /// <summary>Runs every section and returns the entries, each stamped with its section's name.</summary>
    public IReadOnlyList<Lexicon.Entry> Build()
    {
        List.Clear();
        foreach (var (category, section) in Sections())
        {
            int from = List.Count;
            section();
            for (int i = from; i < List.Count; i++) List[i] = List[i] with { Category = category };
        }
        return List;
    }

    /// <summary>
    /// A phrase left exactly as it is, though it holds a listed form: the lore's own "Fallen God" is
    /// not the God that becomes Qoth. Longer than the form inside it, so the match takes it first.
    /// </summary>
    protected void Keep(string phrase)
        => List.Add(new(phrase, phrase, Proper: true, MidSentenceOnly: false));

    /// <summary>A single form, any case.</summary>
    protected void W(string form, string replacement)
        => List.Add(new(form, replacement, Proper: false, MidSentenceOnly: false));

    /// <summary>
    /// A unit of count: the noun and its plural as <see cref="N"/>, and after a number or a
    /// quantifier the whole count is rewritten by <paramref name="counted"/> ("six centuries" →
    /// "six hundred rounds", "a month" → "thirty days").
    /// </summary>
    protected void C(string noun, string replacement, string plural, string replacementPlural,
                          Func<Lexicon.Quantity, string> counted)
    {
        List.Add(new(noun, replacement, Proper: false, MidSentenceOnly: false, counted));
        List.Add(new(plural, replacementPlural, Proper: false, MidSentenceOnly: false, counted));
    }

    /// <summary>A count of <paramref name="per"/> <paramref name="unit"/> per item: two months → sixty days.</summary>
    protected static Func<Lexicon.Quantity, string> Times(long per, string unit, string otherwise)
        => q => q.Per ? $"in {Lexicon.NumberWords(per)} {unit}"
            : q.Value is long n ? $"{Lexicon.Number(n * per, q)} {unit}"
            : $"{q.Text} {otherwise}";

    /// <summary>
    /// A count kept as written before a larger number: six centuries → six hundred rounds, a few
    /// centuries → a few hundred rounds. Digits are multiplied out (3 centuries → 300 rounds).
    /// </summary>
    protected static Func<Lexicon.Quantity, string> Before(long scale, string scaleWord, string unit)
        => q => q.Per ? $"in a {scaleWord} {unit}"
            : q.Value is long n && char.IsDigit(q.Text[0]) ? $"{n * scale} {unit}"
            : $"{q.Text} {scaleWord} {unit}";

    /// <summary>Several forms with one replacement.</summary>
    protected void Ws(string forms, string replacement)
    {
        foreach (var f in Split(forms)) W(f, replacement);
    }

    /// <summary>A noun and its plural. Irregular plurals are given explicitly.</summary>
    protected void N(string noun, string replacement, string? plural = null, string? replacementPlural = null)
    {
        List.Add(new(noun, replacement, Proper: false, MidSentenceOnly: false) { Lemma = noun.ToLowerInvariant() });
        // An invariant plural ("aircraft", "haiku") is the singular's own form, which keeps the
        // singular replacement: the plural reading cannot be told from it.
        string p = plural ?? Plural(noun);
        if (!p.Equals(noun, StringComparison.OrdinalIgnoreCase))
            List.Add(new(p, replacementPlural ?? Plural(replacement), Proper: false, MidSentenceOnly: false)
                     { Lemma = noun.ToLowerInvariant() });
    }

    /// <summary>Paired forms: "a,b,c" → "x,y,z", one for one (a verb's base, -s, past, -ing).</summary>
    protected void V(string forms, string replacements)
    {
        var f = Split(forms);
        var r = Split(replacements);
        if (f.Length != r.Length)
            throw new InvalidOperationException($"{GetType().Name}: '{forms}' and '{replacements}' differ in length.");
        for (int i = 0; i < f.Length; i++) W(f[i], r[i]);
    }

    /// <summary>A proper noun, matched only as capitalised; its replacement keeps its own case.</summary>
    protected void P(string name, string replacement, bool midSentenceOnly = false)
        => List.Add(new(name, replacement, Proper: true, MidSentenceOnly: midSentenceOnly));

    /// <summary>Several proper nouns with one replacement.</summary>
    protected void Ps(string names, string replacement)
    {
        foreach (var n in Split(names)) P(n, replacement);
    }

    /// <summary>A demonym: "Roman" and "Romans", as adjective and as people.</summary>
    protected void D(string name, string replacement, string? plural = null, string? replacementPlural = null)
    {
        P(name, replacement);
        P(plural ?? name + "s", replacementPlural ?? Plural(replacement));
    }

    protected static string[] Split(string s)
        => s.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private static readonly Dictionary<string, string> _irregular = new(StringComparer.OrdinalIgnoreCase)
    {
        ["man"] = "men", ["woman"] = "women", ["foot"] = "feet", ["tooth"] = "teeth", ["child"] = "children",
        ["knife"] = "knives", ["leaf"] = "leaves", ["life"] = "lives", ["wife"] = "wives", ["mouse"] = "mice",
        ["person"] = "people", ["fish"] = "fish", ["sheep"] = "sheep", ["deer"] = "deer", ["craft"] = "craft",
        ["shelf"] = "shelves", ["loaf"] = "loaves", ["thief"] = "thieves", ["staff"] = "staves",
        ["cloth"] = "cloths", ["ox"] = "oxen", ["goose"] = "geese", ["day"] = "days", ["key"] = "keys",
        ["lore"] = "lore", ["chapter"] = "chapters", ["armour"] = "armour", ["grain"] = "grains",
        ["stuff"] = "stuff", ["swine"] = "swine", ["gallows"] = "gallows",
    };

    /// <summary>English plural of a noun or of a noun phrase's head ("span of days" → "spans of days").</summary>
    internal static string Plural(string phrase)
    {
        int of = phrase.IndexOf(" of ", StringComparison.Ordinal);
        if (of > 0) return Plural(phrase[..of]) + phrase[of..];

        int sp = phrase.LastIndexOf(' ');
        string head = sp < 0 ? phrase : phrase[(sp + 1)..];
        string lead = sp < 0 ? "" : phrase[..(sp + 1)];

        int hy = head.LastIndexOf('-');
        string pre = hy < 0 ? "" : head[..(hy + 1)];
        string w   = hy < 0 ? head : head[(hy + 1)..];

        string p;
        if (_irregular.TryGetValue(w, out var irr))
            p = char.IsUpper(w[0]) ? char.ToUpperInvariant(irr[0]) + irr[1..] : irr;
        else if (w.EndsWith("man", StringComparison.Ordinal) && w.Length > 3)
            p = w[..^3] + "men";
        else if (w.Length > 1 && w.EndsWith('y') && "aeiou".IndexOf(w[^2]) < 0)
            p = w[..^1] + "ies";
        else if (w.EndsWith('s') || w.EndsWith('x') || w.EndsWith('z') || w.EndsWith("ch") || w.EndsWith("sh"))
            p = w + "es";
        else
            p = w + "s";
        return lead + pre + p;
    }

}
