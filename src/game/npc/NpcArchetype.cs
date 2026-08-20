using System.Collections.Generic;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Npc;

/// <summary>
/// Abstract base for all NPC templates — both named (anatomy-bearing) and shallow (anonymous, lootable).
/// Concrete archetypes descend from <see cref="NamedNpcArchetype"/> or <see cref="ShallowNpcArchetype"/>.
/// </summary>
public abstract class NpcArchetype
{
    /// <summary>Archetype identifier (e.g. "wolf", "druid", "chicken").</summary>
    public abstract string ArchetypeId { get; }

    /// <summary>
    /// Which of the four senses this creature rewards being turned on it — the same declaration a
    /// <see cref="PointOfInterest"/> carries, and read through the same <c>SensoryVerb</c> gate.
    ///
    /// <para>Until this existed the senses could not touch a living thing at all: the gate tested
    /// <c>target is PointOfInterest</c>, so the game let you listen to a tree and not to the lark in
    /// it. Every bird, insect and beast in the game was scenery you could kill and not look at.</para>
    ///
    /// <para>Defaults to a close look and nothing else — the safe reading for any creature nobody has
    /// thought about. Override where the creature is worth hearing (birds, crickets, bees) or
    /// smelling (livestock), and see <see cref="NamedNpcArchetype"/>, which rewards all four because
    /// a person is the richest thing in any room.</para>
    /// </summary>
    public virtual SensoryProfile Senses => SensoryProfile.Examinable;

    /// <summary>
    /// Per-verb modus mentis overrides for this kind of creature, keyed by verb id — the NPC side of
    /// <see cref="IVerbModusMentisSource"/>, reached through <see cref="SceneNpc"/>.
    ///
    /// <para>This is what makes listening to a lark a different lesson from listening to a mill race.
    /// Left null by most archetypes, which are happy with whatever the verb teaches.</para>
    /// </summary>
    public virtual IReadOnlyDictionary<string, string>? VerbModiMentis => null;

    /// <summary>The lesson this creature teaches for <paramref name="verbId"/>, or null for no opinion.</summary>
    public string? ModusMentisFor(string verbId)
        => VerbModiMentis != null && VerbModiMentis.TryGetValue(verbId, out var id) ? id : null;

    /// <summary>
    /// The default lessons for turning a sense on an animal — shared by every non-human archetype,
    /// shallow or named, so a wolf, a lark and a beetle all teach the naturalist's knowledge rather
    /// than the object lessons (scrutiny, an eye for beauty) the verbs default to.
    ///
    /// <para>Listening is deliberately left out here: what a creature sounds like is the most
    /// specific of the four, so it is declared per archetype (birds get <c>birdsong</c>) and falls
    /// back to <c>keen_ear</c> for anything nobody has written a voice for.</para>
    /// </summary>
    protected static readonly IReadOnlyDictionary<string, string> CreatureSenses =
        new Dictionary<string, string>
        {
            ["examine"]     = "creature_lore",
            ["smell"]       = "musk_reading",
            ["contemplate"] = "fellow_feeling",
        };

    /// <summary><see cref="CreatureSenses"/> plus a voice worth telling apart — the birds.</summary>
    protected static readonly IReadOnlyDictionary<string, string> SingingCreatureSenses =
        new Dictionary<string, string>
        {
            ["examine"]     = "creature_lore",
            ["smell"]       = "musk_reading",
            ["contemplate"] = "fellow_feeling",
            ["listen"]      = "birdsong",
        };
}
