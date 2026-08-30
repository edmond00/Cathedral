using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game;

/// <summary>
/// Party-fill testing mode: when <c>--fill-party</c> is passed on the command line, the protagonist's
/// roster is filled to its heart-derived ceiling (the <c>max_companions</c> stat) with freshly
/// generated NPCs, right after the childhood phase ends. The last slot is a beast; every slot before
/// it is a human, so a filled party always exercises both kinds.
///
/// <para>For testing only, and inert unless the flag is passed. Recruiting a companion in play means
/// finding somebody, talking them round and passing a check (or, for a beast, appeasing and taming
/// it) — a long approach for anything that merely wants a populated party: the party panel's slot
/// switching, the fight builder's ally loop, name faking over several characters, the travel-weight
/// blocker, and the over-cap dismissal screen all need one before they do anything at all.</para>
/// </summary>
public static class FillPartyMode
{
    public static bool IsActive { get; set; } = false;

    /// <summary>Master-seeded: which archetypes are drawn is part of what <c>--seed</c> pins.</summary>
    private static readonly Random _rng = GameRng.Stream("fill-party");

    /// <summary>
    /// Fills <paramref name="protagonist"/>'s roster up to <c>max_companions</c>, generating each
    /// member from a randomly drawn archetype. No-op when the flag is off, when the heart affords no
    /// companions, or when the party is already full.
    /// </summary>
    public static void FillIfActive(Protagonist protagonist)
    {
        if (!IsActive) return;

        int max     = Cathedral.Game.Scene.Verbs.TameVerb.MaxCompanions(protagonist);
        int missing = max - protagonist.CompanionParty.Count;
        if (missing <= 0)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"*** --fill-party: nothing to do (party {protagonist.CompanionParty.Count}/{max}) ***");
            Console.ResetColor();
            return;
        }

        // The beast takes the last slot, so the humans are however many come before it. A ceiling of
        // one therefore yields a beast and nothing else.
        var chosen = new List<NamedNpcArchetype>();
        chosen.AddRange(Draw(HumanArchetypes(), missing - 1));
        chosen.AddRange(Draw(BeastArchetypes(), 1));

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"*** --fill-party: filling {chosen.Count} companion slot(s) ***");

        foreach (var archetype in chosen)
        {
            // Spawn the whole NPC and keep its body, exactly as recruitment does: an NpcEntity wraps
            // an EnemyCombatant, which is a PartyMember, so this is the same object a recruited
            // villager would join as — organs, skills, belongings, age and all.
            var npc = archetype.Spawn(_rng);
            protagonist.CompanionParty.Add(npc.Combatant);
            Console.WriteLine($"  {npc.Combatant.DisplayName} ({archetype.ArchetypeId}, "
                + $"{archetype.Species.DisplayName}) joined");
        }

        Console.WriteLine($"*** --fill-party: party is {protagonist.CompanionParty.Count}/{max} ***");
        Console.ResetColor();
    }

    /// <summary>
    /// Takes <paramref name="count"/> archetypes at random, without replacement so a filled party is
    /// not three of the same trade. Falls back to drawing with replacement once the pool runs out,
    /// and returns nothing at all when there is no pool (or nothing was asked for).
    /// </summary>
    private static IEnumerable<NamedNpcArchetype> Draw(IReadOnlyList<NamedNpcArchetype> pool, int count)
    {
        if (count <= 0 || pool.Count == 0) yield break;

        var shuffled = pool.OrderBy(_ => _rng.Next()).ToList();
        for (int i = 0; i < count; i++)
            yield return i < shuffled.Count ? shuffled[i] : pool[_rng.Next(pool.Count)];
    }

    /// <summary>Archetypes whose species is human — everyone who joins as a person.</summary>
    private static IReadOnlyList<NamedNpcArchetype> HumanArchetypes() =>
        AllArchetypes().Where(a => a.Species.AnatomyType == AnatomyType.Human).ToList();

    /// <summary>
    /// Everything else: wolf, bear, boar, fox, the strays. Only <see cref="NamedNpcArchetype"/>s
    /// qualify — a chicken is a <c>ShallowNpcArchetype</c> and has no body to join with.
    /// </summary>
    private static IReadOnlyList<NamedNpcArchetype> BeastArchetypes() =>
        AllArchetypes().Where(a => a.Species.AnatomyType != AnatomyType.Human).ToList();

    private static List<NamedNpcArchetype>? _all;

    /// <summary>
    /// Every concrete archetype in the assembly, discovered by reflection (the same pattern as
    /// <c>VerbRegistry</c> and <c>ItemRegistry</c>) so an archetype written tomorrow is drawable
    /// without touching this file.
    /// </summary>
    private static List<NamedNpcArchetype> AllArchetypes() => _all ??=
        Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(NamedNpcArchetype)) && !t.IsAbstract
                     && t.GetConstructor(Type.EmptyTypes) != null)
            .Select(t => (NamedNpcArchetype)Activator.CreateInstance(t)!)
            .OrderBy(a => a.ArchetypeId, StringComparer.Ordinal)   // stable order → seed-stable draws
            .ToList();
}
