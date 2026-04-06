using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc;

/// <summary>
/// An <see cref="ObservationObject"/> wrapping a live <see cref="NpcEntity"/>.
/// Placed into a <see cref="NarrationNode.PossibleOutcomes"/> by
/// <see cref="NarrationGraph.TimeUpdate"/> based on the NPC's schedule.
///
/// Observation keywords come from the NPC's own <see cref="NpcEntity.NarrationKeywordsInContext"/>
/// (visual description: "a grey wolf lurking nearby", "yellow eyes gleaming…", etc.).
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
        if (npc.CanDialogue)
            subs.Add(new DialogueOutcome(npc));
        subs.Add(new FightOutcome(npc));
        SubOutcomes = subs;
    }

    // ── ObservationObject overrides ───────────────────────────────────────────

    /// <summary>Stable id derived from the NPC's own id.</summary>
    public override string ObservationId => $"npc_{Npc.NpcId}";

    /// <summary>
    /// Keywords come directly from the NPC entity — the visual/sensory description
    /// of this creature or person (e.g. "a grey wolf lurking nearby").
    /// </summary>
    public override List<KeywordInContext> ObservationKeywordsInContext
        => new List<KeywordInContext>(Npc.NarrationKeywordsInContext);

    /// <inheritdoc/>
    /// For NPC observations, direct keywords are derived from the archetype id
    /// (e.g. "wolf" from "wolf_12345") so that the LLM-generated text can still
    /// be matched even if the full indirect keywords aren't used verbatim.
    public override List<string> DirectObservationKeywords
    {
        get
        {
            var parts = Npc.Archetype.ArchetypeId
                .Split('_', StringSplitOptions.RemoveEmptyEntries);
            var result = new List<string>();
            foreach (var p in parts)
                if (p.Length > 1)
                    result.Add(p.ToLowerInvariant());
            return result;
        }
    }

    /// <summary>Uses the NPC's observation hint as the neutral scene description.</summary>
    public override string GenerateNeutralDescription(int locationId = 0)
        => Npc.ObservationHint;

    // AssociatedEncounters returns empty — NPCs in the new system are graph-level,
    // not propagated through ObservationObject encounter slots.
}
