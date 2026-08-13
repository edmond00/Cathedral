using System.Collections.Generic;

namespace Cathedral.Game.Save;

/// <summary>
/// One saved run, whole. Five facts, and the survey that produced this list found nothing else that
/// feeds a scene build.
///
/// <para><b>What is absent is the interesting half.</b> No terrain, because the world is a pure
/// function of <see cref="Seed"/>. No scene contents, because a scene is a pure function of its
/// location id. No time of day, because the hour is drawn fresh on every arrival and discarded. No
/// travel range, no derived stats, no current HP — all recomputed from what is here.</para>
/// </summary>
public sealed class SaveGame
{
    /// <summary>
    /// The save format's version. **Bump this whenever the shape of what is written changes**, and
    /// only then — every bump silently invalidates every existing save, which during a playtest means
    /// every tester's run.
    ///
    /// <para>A mismatch is refused silently: Continue simply greys out. There is deliberately no
    /// migration path and no partial read. A save is confined to the build that wrote it, which is
    /// also what lets <see cref="PartyState.Rebuild"/> treat an unknown content id as corruption
    /// rather than as a version difference it should tolerate.</para>
    /// </summary>
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;

    /// <summary>
    /// The run's master seed. Restoring it reproduces the world exactly — terrain, where every
    /// location sits, and the people inside each one — so no part of the map is stored.
    /// </summary>
    public int Seed { get; set; }

    /// <summary>
    /// The world clock. Not optional: nothing stores an age, a wound's progress or an item depletion,
    /// because all three are differences measured against this.
    /// </summary>
    public double Days { get; set; }

    /// <summary>Which vertex the avatar stands on.</summary>
    public int AvatarVertex { get; set; }

    /// <summary>The party — see <see cref="PartyState"/> for the contract.</summary>
    public PartyState Party { get; set; } = new();

    /// <summary>
    /// What each visited location remembers, keyed by vertex. Nested verbatim: this type was written
    /// as plain data for exactly this purpose, and its own doc says a save file reads and writes
    /// exactly its fields.
    /// </summary>
    public Dictionary<int, LocationInstanceState> Locations { get; set; } = new();
}
