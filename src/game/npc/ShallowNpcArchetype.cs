using System;
using System.Text;
using Cathedral;
using Cathedral.Game.Npc.Corpse;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Npc;

/// <summary>
/// Abstract archetype for <b>shallow</b> NPCs — anonymous creatures with no personal anatomy.
/// They cannot fight, join the party, or hold dialogue, but they can be slayed to yield a
/// lootable corpse, and they follow a time-period schedule across scene areas.
/// Spawns <see cref="ShallowNpcEntity"/> instances.
/// </summary>
public abstract class ShallowNpcArchetype : NpcArchetype
{
    /// <summary>Display name used for all instances of this type (e.g. "Chicken", "Rabbit").</summary>
    public abstract string TypeDisplayName { get; }

    // ── Spawn ────────────────────────────────────────────────────────────────

    /// <summary>Spawns a new <see cref="ShallowNpcEntity"/> from this archetype.</summary>
    public ShallowNpcEntity Spawn(Random rng, string nodeContext = "")
    {
        var npcId   = $"{ArchetypeId}_{rng.Next(100000)}";
        // Description RNG is seeded from the NPC id (stable FNV-1a hash) so the same creature always
        // looks the same across a run, independent of the spawn-order rng passed in.
        var descRng = new Random(GameRng.DerivedSeed($"npc_desc_{npcId}"));
        var hint    = ComposeObservationHint(descRng, nodeContext);
        return new ShallowNpcEntity(npcId, TypeDisplayName, this, hint);
    }

    // ── Overridable builders ────────────────────────────────────────────────

    /// <summary>
    /// Override to compose the observation description by drawing from small attribute pools with
    /// <paramref name="rng"/> (which is seeded per-NPC, so the result is stable). Prefer the
    /// <see cref="Compose"/> helper, which assembles a grammatical "a/an {size} {colour} {noun}, {trait}"
    /// sentence. The description is appearance-only — no name (shallow NPCs are anonymous).
    /// </summary>
    protected abstract string ComposeObservationHint(Random rng, string nodeContext);

    /// <summary>Override to build a <see cref="CorpseSpot"/> when an instance is slain.</summary>
    public abstract CorpseSpot CreateCorpse(ShallowNpcEntity entity, Area area);

    // ── Composition helpers ──────────────────────────────────────────────────

    /// <summary>Picks one option from a pool. Draws exactly one value from <paramref name="rng"/>.</summary>
    protected static string Pick(Random rng, params string[] options) => options[rng.Next(options.Length)];

    /// <summary>
    /// Assembles a grammatical creature description from optional size/colour pools, a fixed noun, and
    /// an optional trait/activity pool: <c>"a small brown crab, one claw held high"</c>. The article is
    /// chosen (a/an) from the first word, so vowel-initial descriptors are safe. Pools are drawn in a
    /// fixed order (size → colour → trait); pass an empty array to skip a dimension. Any pool may be
    /// omitted, but keep the call order stable so a given NPC id stays deterministic.
    /// </summary>
    protected static string Compose(Random rng, string[] sizes, string[] colors, string noun, string[] traits)
    {
        var head = new StringBuilder();
        if (sizes.Length  > 0) head.Append(Pick(rng, sizes)).Append(' ');
        if (colors.Length > 0) head.Append(Pick(rng, colors)).Append(' ');
        head.Append(noun);

        string h       = head.ToString();
        string article = "aeiou".IndexOf(char.ToLowerInvariant(h[0])) >= 0 ? "an" : "a";
        string trait   = traits.Length > 0 ? $", {Pick(rng, traits)}" : "";
        return $"{article} {h}{trait}";
    }
}
