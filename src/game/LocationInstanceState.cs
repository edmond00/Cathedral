using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cathedral.Game.Dialogue.Affinity;

namespace Cathedral.Game;

/// <summary>
/// Everything about one location that outlives the visit.
///
/// <para>A scene is <b>rebuilt from its factory on every arrival</b> and thrown away on leaving —
/// deterministically, since <c>SceneFactory.CreateSeededRandom</c> is seeded by the location id
/// alone, so the same rooms, the same people and the same animals come back identical every time.
/// Everything the player <i>changed</i> therefore has to live here, or it did not happen. Corpses,
/// wounds dealt, doors forced and the rest are deliberately not here: they are one visit's business.
/// </para>
///
/// <para><b>The contract for anything added to this class</b> — it is short, and worth keeping short,
/// because a save file will read and write exactly these fields:</para>
///
/// <list type="number">
/// <item><b>Plain data only.</b> Dictionaries, sets and lists of primitives or enums. No object
/// references, no <c>Guid</c>s, nothing that means something only inside one built scene — a
/// <c>Guid</c> is re-minted by the next build and would point at nothing. This is what makes the
/// class round-trip through <see cref="ToJson"/> with no custom converter.</item>
/// <item><b>Keyed by a stable id.</b> The key must name the same thing in the next build:
/// <see cref="Npc.INpcEntity.PersistentId"/> for a person or an animal,
/// <c>ItemElement.DepletionKey</c> for a picked slot. Both are derived from build-order-stable data,
/// not from run-time identity.</item>
/// <item><b>Mutable in place, shared by reference.</b> <see cref="AttachTo"/> hands the live
/// collections to the scene, so gameplay writes land here directly and persist with no save step.
/// That is why this is a class and not a record: <c>with</c>-copies would silently detach a store
/// the scene is still writing to.</item>
/// <item><b>Wired in one place.</b> Add the property, then add its line to <see cref="AttachTo"/>.
/// Nothing else should reach into a location's state to hand a store to a scene.</item>
/// </list>
/// </summary>
public sealed class LocationInstanceState
{
    /// <summary>World vertex this state belongs to, as a string.</summary>
    public string LocationId { get; init; } = "";

    /// <summary>Biome or location name it was built from ("plain", "village", …).</summary>
    public string LocationType { get; init; } = "";

    // ── Persisted facts ───────────────────────────────────────────────────────

    /// <summary>
    /// Per-NPC affinity, keyed by <see cref="Npc.INpcEntity.PersistentId"/>. Each inner dictionary is
    /// the backing store of that NPC's <see cref="AffinityTable"/>, so a dialogue that changes how
    /// someone feels about you writes straight through to here.
    ///
    /// <para>The key must be per NPC, not per role: keying by archetype alone once made every
    /// same-role NPC in a location share one table, so befriending one plowman befriended them
    /// all.</para>
    /// </summary>
    public Dictionary<string, Dictionary<string, AffinityLevel>> NpcAffinity { get; init; } = new();

    /// <summary>
    /// Who each NPC counts an enemy, keyed by <see cref="Npc.INpcEntity.PersistentId"/> and holding
    /// <c>PartyMember.AffinityKey</c>s. The backing store of that NPC's
    /// <see cref="AffinityTable"/> enemy set, so a fight declared during a visit is still declared on
    /// the next one.
    ///
    /// <para>Separate from <see cref="NpcAffinity"/> because enmity is not a point on the affinity
    /// ladder — an enemy has an affinity level as well, and the two move independently (the reconcile
    /// tree clears the flag and leaves you Suspicious). Keeping enmity per-visit made running away the
    /// dominant answer to every fight: the scene was rebuilt on arrival, the grudge was not rebuilt
    /// with it, and the man who drew steel on you last time had no memory of it.</para>
    /// </summary>
    public Dictionary<string, HashSet<string>> NpcEnemies { get; init; } = new();

    /// <summary>
    /// Item-depletion timestamps: <c>ItemElement.DepletionKey</c> → the <c>GameClock.Days</c> at
    /// which that slot was last picked. A slot is depleted while <c>now − pickedAt &lt;
    /// PoI.RegenDays</c>, so a berry bush stripped bare stays bare for a while and then does not.
    /// </summary>
    public Dictionary<string, double> ItemDepletions { get; init; } = new();

    /// <summary>
    /// NPCs that are gone from this location for good, by
    /// <see cref="Npc.INpcEntity.PersistentId"/> — killed, or taken into the party by taming or by
    /// agreement.
    ///
    /// <para>Without this the rebuild brings them straight back, because it is a pure function of the
    /// location id: a wolf you tamed would be padding beside you <i>and</i> waiting in the clearing,
    /// and anyone you killed would be alive again on your next visit. Departure is permanent — the
    /// spot in the world stays empty rather than re-rolling a replacement, since a replacement drawn
    /// from the same seed would be the very individual that left.</para>
    /// </summary>
    public HashSet<string> DepartedNpcs { get; init; } = new();

    // ── Wiring ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Hands this location's live stores to a freshly built <paramref name="scene"/>. The scene holds
    /// the same collection objects, not copies, so anything it writes during the visit is already
    /// saved when the player walks out.
    ///
    /// <para>The single seam between persistent state and a scene: a new persisted fact needs one
    /// line here and nothing anywhere else.</para>
    /// </summary>
    public void AttachTo(Scene.Scene scene)
    {
        scene.ItemDepletions = ItemDepletions;
        scene.DepartedNpcs   = DepartedNpcs;
    }

    /// <summary>
    /// The persistent <see cref="AffinityTable"/> for one NPC, wrapping this location's backing store
    /// for <paramref name="persistentId"/> (created on first use — later mutations persist with no
    /// explicit save step). Handed to <c>NamedNpcArchetype.Spawn</c> as its affinity resolver.
    /// </summary>
    public AffinityTable AffinityFor(string persistentId)
    {
        if (!NpcAffinity.TryGetValue(persistentId, out var dict))
            NpcAffinity[persistentId] = dict = new Dictionary<string, AffinityLevel>();
        if (!NpcEnemies.TryGetValue(persistentId, out var enemies))
            NpcEnemies[persistentId] = enemies = new HashSet<string>();
        return new AffinityTable(dict, enemies);
    }

    /// <summary>Records that an NPC has left this location for good.</summary>
    public void MarkDeparted(string persistentId) => DepartedNpcs.Add(persistentId);

    /// <summary>True when this NPC has already left and must not be rebuilt into the scene.</summary>
    public bool HasDeparted(string persistentId) => DepartedNpcs.Contains(persistentId);

    // ── Construction & serialisation ──────────────────────────────────────────

    /// <summary>The state of a location visited for the first time.</summary>
    public static LocationInstanceState ForScene(int vertex, string locationType) => new()
    {
        LocationId   = vertex.ToString(),
        LocationType = locationType,
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters    = { new JsonStringEnumConverter() },
    };

    /// <summary>Serialises the whole state. Every property above is plain data, so this is total.</summary>
    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    /// <summary>Reads a state back. A field absent from an older save keeps its empty default.</summary>
    public static LocationInstanceState? FromJson(string json)
        => JsonSerializer.Deserialize<LocationInstanceState>(json, JsonOptions);

    public override string ToString()
        => $"Location {LocationId} ({LocationType}) — {NpcAffinity.Count} known NPC(s), " +
           $"{NpcEnemies.Values.Count(e => e.Count > 0)} hostile, " +
           $"{DepartedNpcs.Count} departed, {ItemDepletions.Count} depleted slot(s)";
}
