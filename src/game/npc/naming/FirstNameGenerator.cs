using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Npc.Naming;

/// <summary>
/// Builds a fantasy first name: draw a gendered base from <see cref="FirstNameData"/>, then apply a
/// handful of <see cref="NameModifiers"/> to twist its spelling.
/// </summary>
public static class FirstNameGenerator
{
    /// <summary>
    /// Generate a first name for the given sex. One to three modifiers are applied (⅓ each), re-filtered
    /// after every application because a rewrite can enable or disable other rules.
    /// </summary>
    public static string Generate(bool male, Random rng)
    {
        var pool = male ? FirstNameData.Male : FirstNameData.Female;
        string name = pool[rng.Next(pool.Length)];
        return ApplyModifiers(name, rng, count: 1 + rng.Next(3));
    }

    /// <summary>
    /// Generate a name to be reused as a <b>last</b> name: gender-agnostic base (either pool), and as
    /// many modifiers as possible up to three so the surname reads more invented than the given name.
    /// </summary>
    public static string GenerateForLastName(Random rng)
    {
        string name = rng.Next(2) == 0
            ? FirstNameData.Male[rng.Next(FirstNameData.Male.Length)]
            : FirstNameData.Female[rng.Next(FirstNameData.Female.Length)];
        return ApplyModifiers(name, rng, count: 3);
    }

    /// <summary>Apply up to <paramref name="count"/> distinct applicable modifiers, in random order.</summary>
    private static string ApplyModifiers(string name, Random rng, int count)
    {
        var used = new HashSet<NameModifier>();
        for (int i = 0; i < count; i++)
        {
            var candidates = NameModifiers.All.Where(m => !used.Contains(m) && m.CanApply(name)).ToList();
            if (candidates.Count == 0) break;
            var mod = candidates[rng.Next(candidates.Count)];
            name = mod.Apply(name, rng);
            used.Add(mod);
        }
        return name;
    }
}
