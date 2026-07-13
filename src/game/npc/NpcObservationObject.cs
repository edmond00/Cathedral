using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc;

/// <summary>
/// Implemented by observation outcomes that wrap a named NPC and can have their prompt-facing
/// text swapped from the raw proper name to a contextual <c>Label</c>. The controllers (which
/// hold the <see cref="WorldContext"/>, location and acting party member) call
/// <see cref="StampContextLabel"/> before generating prompts; shallow/unlabelled objects no-op.
/// </summary>
public interface INpcContextLabelStampable
{
    void StampContextLabel(PartyMember? actingMember, WorldContext? world, int locationId);
}

/// <summary>
/// An <see cref="ObservationObject"/> wrapping a live <see cref="NpcEntity"/>.
/// Placed into a <see cref="NarrationNode.PossibleOutcomes"/> by
/// <see cref="NarrationGraph.TimeUpdate"/> based on the NPC's schedule.
///
/// Sub-outcomes are chosen by the thinking GOAL LLM and can be:
///   • <see cref="DialogueOutcome"/> — if the NPC is dialogue-capable
///   • <see cref="FightOutcome"/>   — always available
///
/// Both outcomes extend <see cref="ConcreteOutcome"/> (with empty keyword lists)
/// so they satisfy the SubOutcomes list type constraint; goal selection is
/// purely via <see cref="ConcreteOutcome.ToNaturalLanguageString"/>.
/// </summary>
public class NpcObservationObject : ObservationObject, INpcContextLabelStampable
{
    public NpcEntity Npc { get; }

    /// <summary>
    /// Contextual label (relation + role + location) substituted for the proper name in LLM
    /// prompts. Null until a controller stamps it; reads fall back to <see cref="NpcEntity.DisplayName"/>.
    /// </summary>
    public string? ContextLabel { get; private set; }

    public NpcObservationObject(NpcEntity npc)
    {
        Npc = npc;

        var subs = new List<ConcreteOutcome>();
        if (npc.CanSpeak)
            subs.Add(new DialogueOutcome(npc));
        subs.Add(new FightOutcome(npc));
        SubOutcomes = subs;
    }

    /// <inheritdoc/>
    public void StampContextLabel(PartyMember? actingMember, WorldContext? world, int locationId)
    {
        ContextLabel = NpcLabelResolver.Resolve(Npc, world, locationId, actingMember);
        foreach (var sub in SubOutcomes)
        {
            if (sub is DialogueOutcome d) d.ContextLabel = ContextLabel;
            else if (sub is FightOutcome f) f.ContextLabel = ContextLabel;
        }
    }

    // ── ObservationObject overrides ───────────────────────────────────────────

    /// <summary>Stable id derived from the NPC's own id.</summary>
    public override string ObservationId => $"npc_{Npc.NpcId}";

    /// <summary>The NPC's contextual label if stamped, else its display name (e.g. "Hugh Furrow").</summary>
    public override string NeutralName => ContextLabel ?? Npc.DisplayName;

    /// <summary>Contextual label if stamped, else the proper name — used verbatim in sentences.</summary>
    public override string NeutralPhrase => ContextLabel ?? Npc.DisplayName;

    /// <summary>Names aren't in the embedding vocab — anchor on a generic noun.</summary>
    public override string ReferenceLemma => "person";

    /// <summary>Uses the NPC's observation hint as the neutral scene description.</summary>
    public override string GenerateNeutralDescription(int locationId = 0)
        => Npc.ObservationHint;

    // AssociatedEncounters returns empty — NPCs in the new system are graph-level,
    // not propagated through ObservationObject encounter slots.
}
