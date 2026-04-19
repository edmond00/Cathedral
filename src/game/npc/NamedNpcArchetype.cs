using System;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc;

/// <summary>
/// Abstract archetype for <b>named</b> NPCs — characters with their own anatomy, derived stats,
/// and optional dialogue capability. Can fight, join the party, and engage in conversation.
/// Spawns <see cref="NpcEntity"/> instances.
/// </summary>
public abstract class NamedNpcArchetype : NpcArchetype
{
    /// <summary>Species used for anatomy and combat.</summary>
    public abstract Species Species { get; }

    /// <summary>Whether spawned NPCs are hostile by default.</summary>
    public abstract bool DefaultHostile { get; }

    /// <summary>
    /// Whether spawned NPCs start as enemies of the protagonist (e.g. bears, wolves, bandits).
    /// When true, the enemy flag is set in AffinityTable at scene initialization.
    /// </summary>
    public virtual bool DefaultEnemy => false;

    /// <summary>Whether spawned NPCs persist across visits (named characters).</summary>
    public abstract bool DefaultPersistent { get; }

    /// <summary>Pool of possible display names. One is picked randomly on spawn.</summary>
    public abstract string[] NamePool { get; }

    /// <summary>How many modiMentis to assign at creation.</summary>
    public virtual int ModiMentisCount => 8;

    /// <summary>
    /// Whether spawned NPCs can be spoken to.
    /// Subclasses that provide a <see cref="GenerateWayToSpeakDescription"/> should override this to true.
    /// </summary>
    public virtual bool CanSpeak => false;

    /// <summary>
    /// Whether spawned NPCs confront criminals bravely (demand fight) rather than submitting.
    /// Override to true for guards, owners, or aggressive archetypes.
    /// </summary>
    public virtual bool IsBrave => false;

    /// <summary>
    /// Relative authority level (0 = civilian, higher = more official enforcement power).
    /// Override in guard/lawkeeper archetypes.
    /// </summary>
    public virtual int AuthorityLevel => 0;

    /// <summary>
    /// Section ids that spawned NPCs own by default.
    /// Override to list section ids this archetype has authority over (e.g. farmhouse interior).
    /// </summary>
    public virtual IReadOnlyList<string> DefaultOwnedSectionIds => [];

    // ── Spawn ─────────────────────────────────────────────────────────────────

    /// <summary>Spawns a new <see cref="NpcEntity"/> from this archetype.</summary>
    public NpcEntity Spawn(Random rng, string nodeContext = "", AffinityTable? savedAffinity = null)
    {
        var name = NamePool[rng.Next(NamePool.Length)];
        var npcId = DefaultPersistent
            ? $"{ArchetypeId}_{name.ToLowerInvariant().Replace(' ', '_')}"
            : $"{ArchetypeId}_{rng.Next(100000)}";

        var combatant = new EnemyCombatant(name, Species);
        combatant.InitializeModiMentis(ModusMentisRegistry.Instance, ModiMentisCount);

        var hint           = BuildObservationHint(name, nodeContext);
        var wayToSpeak     = CanSpeak ? GenerateWayToSpeakDescription(name, rng) : null;
        var affinityTable  = savedAffinity ?? new AffinityTable();

        return new NpcEntity(
            npcId, combatant, this,
            DefaultHostile, DefaultPersistent,
            hint,
            canSpeak:              CanSpeak,
            wayToSpeakDescription: wayToSpeak,
            affinityTable:         affinityTable,
            isBrave:               IsBrave,
            authorityLevel:        AuthorityLevel,
            ownedSectionIds:       DefaultOwnedSectionIds);
    }

    // ── Overridable builders ──────────────────────────────────────────────────

    /// <summary>Override to provide the observation hint sentence.</summary>
    protected abstract string BuildObservationHint(string name, string nodeContext);

    /// <summary>
    /// Override to return a natural-language description of how this NPC speaks.
    /// This text is used as the LLM system prompt for the NPC's dialogue slot.
    /// Only called when <see cref="CanSpeak"/> is true.
    /// </summary>
    protected virtual string GenerateWayToSpeakDescription(string name, Random rng) => string.Empty;
}
