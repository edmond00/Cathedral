using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc;

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
public class NpcObservationObject : ObservationObject
{
    public NpcEntity Npc { get; }

    public NpcObservationObject(NpcEntity npc)
    {
        Npc = npc;

        var subs = new List<ConcreteOutcome>();
        if (npc.CanSpeak)
            subs.Add(new DialogueOutcome(npc));
        subs.Add(new FightOutcome(npc));
        SubOutcomes = subs;
    }

    // ── ObservationObject overrides ───────────────────────────────────────────

    /// <summary>Stable id derived from the NPC's own id.</summary>
    public override string ObservationId => $"npc_{Npc.NpcId}";

    /// <summary>Uses the NPC's observation hint as the neutral scene description.</summary>
    public override string GenerateNeutralDescription(int locationId = 0)
        => Npc.ObservationHint;

    // AssociatedEncounters returns empty — NPCs in the new system are graph-level,
    // not propagated through ObservationObject encounter slots.
}
