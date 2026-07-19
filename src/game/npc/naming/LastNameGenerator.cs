using System;

namespace Cathedral.Game.Npc.Naming;

/// <summary>
/// Builds a human last name by picking one of several byname patterns and filling it from
/// <see cref="LastNameData"/> (and, for the "F" patterns, <see cref="FirstNameGenerator"/>).
///
/// Patterns: compound "XY" (Blackwood), "the X" (the Strong), a reused first name "F" (Klarice),
/// two joined first names "F-F" (Aelric-Godwin), "of the Y" (of the Fen), a Roman numeral "N" (IV),
/// combinations of those, or nothing at all (first name only).
/// </summary>
public static class LastNameGenerator
{
    // Weighted kinds. Weights are relative; "None" is common so plenty of folk go by one name.
    private enum Kind
    {
        None, Compound, TheAdjective, FirstAsLast, FirstHyphenFirst,
        OfThePlace, Numeral, NumeralTheAdjective, FirstTheAdjective,
        NumeralOfThePlace, FirstOfThePlace,
    }

    private static readonly (Kind Kind, int Weight)[] Weights =
    {
        (Kind.None,                6),
        (Kind.Compound,           10),
        (Kind.TheAdjective,        6),
        (Kind.FirstAsLast,         5),
        (Kind.FirstHyphenFirst,    3),
        (Kind.OfThePlace,          5),
        (Kind.Numeral,             2),
        (Kind.NumeralTheAdjective, 1),
        (Kind.FirstTheAdjective,   2),
        (Kind.NumeralOfThePlace,   1),
        (Kind.FirstOfThePlace,     2),
    };

    /// <summary>Returns a last name, or the empty string for the "no surname" case.</summary>
    public static string Generate(Random rng)
        => Build(PickKind(rng), rng);

    private static string Build(Kind kind, Random rng) => kind switch
    {
        Kind.None                => "",
        Kind.Compound            => Compound(rng),
        Kind.TheAdjective        => $"the {Adjective(rng)}",
        Kind.FirstAsLast         => FirstNameGenerator.GenerateForLastName(rng),
        Kind.FirstHyphenFirst    => $"{FirstNameGenerator.GenerateForLastName(rng)}-{FirstNameGenerator.GenerateForLastName(rng)}",
        Kind.OfThePlace          => $"of the {Place(rng)}",
        Kind.Numeral             => Numeral(rng),
        Kind.NumeralTheAdjective => $"{Numeral(rng)} the {Adjective(rng)}",
        Kind.FirstTheAdjective   => $"{FirstNameGenerator.GenerateForLastName(rng)} the {Adjective(rng)}",
        Kind.NumeralOfThePlace   => $"{Numeral(rng)} of the {Place(rng)}",
        Kind.FirstOfThePlace     => $"{FirstNameGenerator.GenerateForLastName(rng)} of the {Place(rng)}",
        _                        => "",
    };

    // ── Pieces ────────────────────────────────────────────────────────────────

    private static string Compound(Random rng)
        => LastNameData.CompoundHeads[rng.Next(LastNameData.CompoundHeads.Length)]
         + LastNameData.CompoundTails[rng.Next(LastNameData.CompoundTails.Length)];

    private static string Adjective(Random rng)
        => LastNameData.Adjectives[rng.Next(LastNameData.Adjectives.Length)];

    private static string Place(Random rng)
        => LastNameData.PlaceNouns[rng.Next(LastNameData.PlaceNouns.Length)];

    private static string Numeral(Random rng)
        => ToRoman(rng.Next(2, 15)); // II .. XIV — dynastic bynames, never "I"

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Kind PickKind(Random rng)
    {
        int total = 0;
        foreach (var (_, w) in Weights) total += w;
        int roll = rng.Next(total);
        foreach (var (kind, w) in Weights)
        {
            if (roll < w) return kind;
            roll -= w;
        }
        return Kind.None;
    }

    /// <summary>Small-value integer → Roman numeral (used for dynastic bynames like "the IV").</summary>
    private static string ToRoman(int value)
    {
        (int V, string S)[] table =
        {
            (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I"),
        };
        var sb = new System.Text.StringBuilder();
        foreach (var (v, s) in table)
            while (value >= v) { sb.Append(s); value -= v; }
        return sb.ToString();
    }
}
