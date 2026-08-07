using System.Collections.Generic;
using Cathedral.Game.Npc.Corpse;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Npc;

/// <summary>
/// Shared contract for both <see cref="NpcEntity"/> (named, anatomy-bearing) and
/// <see cref="ShallowNpcEntity"/> (anonymous, lootable-only) instances.
/// </summary>
public interface INpcEntity
{
    /// <summary>Per-build identifier, used for disambiguation inside one scene.</summary>
    string NpcId { get; }

    /// <summary>
    /// The id that names <b>this individual</b> across rebuilds of the location — the key everything
    /// in <see cref="Cathedral.Game.LocationInstanceState"/> is filed under.
    ///
    /// <para>Not <see cref="NpcId"/>, which for a non-persistent NPC carries a random number and so
    /// names a different string every build. This is derived from the archetype and the generated
    /// name, both of which are functions of the location seed, so the same creature answers to the
    /// same id on every visit.</para>
    /// </summary>
    string PersistentId { get; }

    /// <summary>Display name shown in narration and UI.</summary>
    string DisplayName { get; }

    /// <summary>
    /// Whether this NPC is still alive. Setting to false kills the NPC.
    /// Dead NPCs are hidden from scene observations and cannot be interacted with.
    /// </summary>
    bool IsAlive { get; set; }

    /// <summary>Short LLM observation hint (e.g. "a grey wolf watches from the shadows").</summary>
    string ObservationHint { get; }

    /// <summary>The archetype that spawned this entity.</summary>
    NpcArchetype Archetype { get; }

    /// <summary>Human-readable species name for display purposes (e.g. "Human", "Wolf", "Chicken").</summary>
    string SpeciesName { get; }

    /// <summary>
    /// The remains this NPC leaves where it died: a <see cref="CorpsePointOfInterest"/>, and for a
    /// human a second PoI holding what they carried. Added to the area at runtime and not persisted
    /// between scenes — the scene is rebuilt on every arrival, and bodies do not survive that.
    /// </summary>
    List<PointOfInterest> GenerateCorpse();
}
