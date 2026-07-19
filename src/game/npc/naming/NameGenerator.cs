using System;

namespace Cathedral.Game.Npc.Naming;

/// <summary>
/// Facade for procedural NPC names. Callers seed a <see cref="Random"/> deterministically per NPC
/// (see <c>NamedNpcArchetype.Spawn</c>) and ask for a human or beast name.
/// </summary>
public static class NameGenerator
{
    /// <summary>
    /// Full human name: a fantasy first name plus an optional byname/surname. When the last-name
    /// generator rolls "no surname", the result is just the first name.
    /// </summary>
    public static string GenerateHuman(bool male, Random rng)
    {
        string first = FirstNameGenerator.Generate(male, rng);
        string last  = LastNameGenerator.Generate(rng);
        return last.Length == 0 ? first : $"{first} {last}";
    }

    /// <summary>Descriptive beast name — an adjective glued to a beast noun (Sharptooth, Greyfur).</summary>
    public static string GenerateBeast(Random rng)
        => BeastNameData.Adjectives[rng.Next(BeastNameData.Adjectives.Length)]
         + BeastNameData.Nouns[rng.Next(BeastNameData.Nouns.Length)];
}
