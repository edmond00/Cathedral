using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Scene-wide "false name" mapping. The local LLM (and the sanitizer) only ever see short, simple,
/// allow-listed placeholder names ("Thomas", "Margaret") instead of the invented in-world names
/// ("Godric Reeve"), which small models handle poorly and which the WikiNER sanitizer layer would
/// otherwise flag as real-world people. Real names are swapped back in as the very last step of every
/// LLM output.
///
/// <para>One <see cref="NameFakingRegistry"/> is built per scene (protagonist + party companions +
/// every scene NPC) and published on <see cref="Current"/>. Call sites use the null-safe statics
/// <see cref="Fake"/> (real → false, for prompts) and <see cref="Real"/> (false → real, for output);
/// when no scene table is active they are the identity, preserving today's behaviour.</para>
/// </summary>
public static class NameFaking
{
    /// <summary>The active scene's mapping, or null (⇒ identity) before any scene begins.</summary>
    public static NameFakingRegistry? Current { get; set; }

    /// <summary>Real → false. Apply to any text handed to the LLM. Null-safe (identity when no scene table).</summary>
    public static string Fake(string text) => Current?.ToFalse(text) ?? text;

    /// <summary>False → real. Apply to every LLM output before it is shown. Null-safe.</summary>
    public static string Real(string text) => Current?.ToReal(text) ?? text;

    // ── Name pools ───────────────────────────────────────────────────────────────
    // Plain, distinctive first names. Deliberately excludes names that are also lowercase common words
    // (Grace, Rose, Hope, Faith, Joy, Dawn, May, June, Mark, Bill, Art, Miles, …) so the word-boundary
    // replacement can never over-match ordinary prose.

    public static readonly string[] MaleNames =
    {
        "James", "John", "Robert", "Michael", "William", "David", "Richard", "Thomas", "Charles",
        "Daniel", "Matthew", "Anthony", "Steven", "Andrew", "Joshua", "Kenneth", "Kevin", "Brian",
        "George", "Edward", "Ronald", "Timothy", "Jason", "Jeffrey", "Nicholas", "Stephen", "Jonathan",
        "Justin", "Scott", "Brandon", "Benjamin", "Samuel", "Gregory", "Alexander", "Patrick", "Frederick",
        "Dennis", "Tyler", "Aaron", "Henry", "Douglas", "Peter", "Adam", "Nathan", "Walter", "Harold",
        "Carl", "Jeremy", "Gerald", "Keith", "Roger", "Arthur", "Lawrence", "Sean", "Ethan", "Albert",
        "Elijah", "Wayne", "Vincent", "Louis", "Russell", "Philip", "Johnny", "Bradley", "Raymond",
        "Gabriel", "Nathaniel", "Leonard", "Everett", "Simon", "Marcus", "Oscar", "Theodore",
    };

    public static readonly string[] FemaleNames =
    {
        "Mary", "Patricia", "Jennifer", "Linda", "Barbara", "Susan", "Jessica", "Sarah", "Karen",
        "Nancy", "Betty", "Sandra", "Margaret", "Ashley", "Kimberly", "Emily", "Donna", "Michelle",
        "Edith", "Amanda", "Melissa", "Deborah", "Stephanie", "Rebecca", "Laura", "Helen", "Cynthia",
        "Kathleen", "Angela", "Shirley", "Anna", "Brenda", "Pamela", "Nicole", "Katherine", "Samantha",
        "Christine", "Rachel", "Carolyn", "Janet", "Maria", "Heather", "Julie", "Victoria", "Kelly",
        "Christina", "Joan", "Evelyn", "Judith", "Megan", "Andrea", "Hannah", "Jacqueline", "Martha",
        "Teresa", "Sara", "Madison", "Frances", "Kathryn", "Janice", "Jean", "Abigail", "Alice",
        "Sophia", "Julia", "Marie", "Olivia", "Emma", "Eleanor", "Charlotte", "Harriet", "Louisa",
    };
}

/// <summary>
/// A built scene mapping: each real character name ↔ a unique gender-appropriate false name.
/// Bidirectional replacement is word-boundary, case-insensitive, longest-source-first (so a multi-word
/// real name is replaced before its first token would be) — mirroring the old DialogueNameTable.
/// </summary>
public sealed class NameFakingRegistry
{
    private sealed record Entry(string Real, string False, bool Male);

    private readonly List<Entry> _entries = new();
    private readonly Random _rng;

    public NameFakingRegistry(int? seed = null)
        => _rng = seed.HasValue ? new Random(seed.Value) : GameRng.Stream("name-faking");

    /// <summary>
    /// Assigns each (real, male) character a unique false name from the gendered pool. Blank or
    /// duplicate real names are skipped. When a pool is exhausted, falls back to "Man N"/"Woman N".
    /// </summary>
    public void Build(IEnumerable<(string Real, bool Male)> characters)
    {
        var usedFalse = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedReal = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var males   = NameFaking.MaleNames.OrderBy(_ => _rng.Next()).ToList();
        var females = NameFaking.FemaleNames.OrderBy(_ => _rng.Next()).ToList();
        int mi = 0, fi = 0, fallback = 0;

        foreach (var (real, male) in characters)
        {
            if (string.IsNullOrWhiteSpace(real) || !usedReal.Add(real)) continue;

            string? chosen = null;
            var pool = male ? males : females;
            ref int idx = ref (male ? ref mi : ref fi);
            while (idx < pool.Count && chosen == null)
            {
                var candidate = pool[idx++];
                if (usedFalse.Add(candidate)) chosen = candidate;
            }
            chosen ??= (male ? "Man" : "Woman") + (++fallback);

            _entries.Add(new Entry(real.Trim(), chosen, male));
        }
    }

    /// <summary>Every false name assigned in this scene (for the sanitizer allow-list).</summary>
    public IReadOnlyCollection<string> FalseNames => _entries.Select(e => e.False).ToList();

    /// <summary>The false name for a real display name, or null when the character is not registered.</summary>
    public string? FalseFor(string real) =>
        _entries.FirstOrDefault(e => string.Equals(e.Real, real, StringComparison.OrdinalIgnoreCase))?.False;

    /// <summary>Real → false: apply to any text handed to the LLM.</summary>
    public string ToFalse(string text) => Replace(text, fromReal: true);

    /// <summary>False → real: apply to every generated text before it is shown.</summary>
    public string ToReal(string text) => Replace(text, fromReal: false);

    private string Replace(string text, bool fromReal)
    {
        if (string.IsNullOrEmpty(text)) return text ?? "";
        // Longest source first so a multi-word real name is replaced before its first token would be.
        foreach (var e in _entries.OrderByDescending(e => (fromReal ? e.Real : e.False).Length))
        {
            string from = fromReal ? e.Real : e.False;
            string to   = fromReal ? e.False : e.Real;
            if (from.Length == 0) continue;
            text = Regex.Replace(text, $@"\b{Regex.Escape(from)}\b", to.Replace("$", "$$"), RegexOptions.IgnoreCase);
        }
        return text;
    }
}
