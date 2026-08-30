using System;
using System.Collections.Generic;

namespace Cathedral.Game.Npc.Naming;

/// <summary>
/// A single reversible-ish spelling tweak that nudges a mundane base name toward a fantasy
/// register — "Clarice → Klarice", "Francesco → Phrancesco", "Cassius → Cassiax".
///
/// A modifier is a guard (<see cref="CanApply"/>) plus a transform (<see cref="Apply"/>).
/// <see cref="FirstNameGenerator"/> filters the <see cref="All"/> list to the applicable ones for a
/// given base, then applies one to three of them. The guard matters: "replace an 'a' with 'ae'"
/// cannot fire on a name with no 'a'.
/// </summary>
public sealed record NameModifier(Func<string, bool> CanApply, Func<string, Random, string> Apply);

/// <summary>
/// The library of ~50 <see cref="NameModifier"/> rules and the small case-preserving helpers that
/// build them. Replacements keep the case of the character they overwrite, so a leading "C → k"
/// yields "K…", not "k…".
/// </summary>
public static class NameModifiers
{
    // ── Case-preserving primitives ───────────────────────────────────────────

    /// <summary>Capitalise <paramref name="s"/> like <paramref name="template"/>'s first char.</summary>
    private static string MatchCase(string s, char template)
        => s.Length > 0 && char.IsUpper(template)
            ? char.ToUpperInvariant(s[0]) + s.Substring(1)
            : s;

    /// <summary>Indices of every (non-overlapping) case-insensitive occurrence of <paramref name="sub"/>.</summary>
    private static List<int> Occurrences(string name, string sub)
    {
        var idx = new List<int>();
        int i = 0;
        while ((i = name.IndexOf(sub, i, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            idx.Add(i);
            i += sub.Length;
        }
        return idx;
    }

    /// <summary>Replace one randomly chosen occurrence of <paramref name="from"/> anywhere in the name.</summary>
    private static NameModifier Replace(string from, string to) => new(
        CanApply: n => n.IndexOf(from, StringComparison.OrdinalIgnoreCase) >= 0,
        Apply: (n, rng) =>
        {
            var spots = Occurrences(n, from);
            if (spots.Count == 0) return n;
            int at = spots[rng.Next(spots.Count)];
            string repl = MatchCase(to, n[at]);
            return n.Substring(0, at) + repl + n.Substring(at + from.Length);
        });

    /// <summary>Replace the trailing <paramref name="from"/> (only when a root remains before it).</summary>
    private static NameModifier ReplaceEnding(string from, string to) => new(
        CanApply: n => n.Length > from.Length && n.EndsWith(from, StringComparison.OrdinalIgnoreCase),
        Apply: (n, _) =>
        {
            int at = n.Length - from.Length;
            string repl = MatchCase(to, n[at]);
            return n.Substring(0, at) + repl;
        });

    /// <summary>Replace the leading <paramref name="from"/> (only when a root remains after it).</summary>
    private static NameModifier ReplaceStart(string from, string to) => new(
        CanApply: n => n.Length > from.Length && n.StartsWith(from, StringComparison.OrdinalIgnoreCase),
        Apply: (n, _) => MatchCase(to, n[0]) + n.Substring(from.Length));

    // ── The library ──────────────────────────────────────────────────────────

    // Rules are deliberately gentle single substitutions: at most three are applied to a name, so a
    // rule that stacks (letter-doubling, blanket "insert an h") quickly turns a name into keyboard
    // mush. The target register is "Clarice → Klarice", "Francesco → Phrancesco" — recognisable roots,
    // lightly re-spelt.
    public static readonly IReadOnlyList<NameModifier> All = new List<NameModifier>
    {
        // Interior substitutions
        Replace("c", "k"),
        Replace("a", "ae"),
        Replace("f", "ph"),
        Replace("l", "lh"),
        Replace("s", "z"),
        Replace("s", "sh"),
        Replace("i", "y"),
        Replace("th", "dh"),
        Replace("v", "w"),
        Replace("g", "gh"),
        Replace("ch", "kh"),
        Replace("qu", "kw"),
        Replace("x", "ks"),
        Replace("ci", "ti"),
        Replace("ce", "se"),
        Replace("ck", "k"),
        Replace("oo", "u"),
        Replace("ee", "ea"),
        Replace("ou", "ow"),

        // Endings
        ReplaceEnding("us", "ax"),
        ReplaceEnding("us", "os"),
        ReplaceEnding("ius", "ios"),
        ReplaceEnding("a", "wyn"),
        ReplaceEnding("o", "oth"),
        ReplaceEnding("e", "ael"),
        ReplaceEnding("ric", "rik"),
        ReplaceEnding("in", "yn"),
        ReplaceEnding("er", "ar"),
        ReplaceEnding("y", "ie"),
        ReplaceEnding("an", "ane"),
        ReplaceEnding("el", "ael"),
        ReplaceEnding("ia", "ya"),
        ReplaceEnding("ard", "arth"),

        // Beginnings — only transforms not already covered by an interior single-letter rule (which
        // matches leading letters too), so we don't double-apply and produce "Ghh…".
        ReplaceStart("Th", "Dh"),
        ReplaceStart("J", "Y"),
        ReplaceStart("Ph", "F"),
        ReplaceStart("W", "Wh"),
    };
}
