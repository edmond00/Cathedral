using System.Collections.Generic;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Outcome that triggers a dialogue with an NPC using a specific dialogue tree.
/// When applied, the narrative controller pauses and enters dialogue mode.
/// Extends ConcreteOutcome so it can appear as a sub-outcome inside NpcObservationObject.
/// </summary>
public class DialogueOutcome : ConcreteOutcome
{
    /// <summary>The NPC to talk to.</summary>
    public NpcEntity Target { get; }

    /// <summary>
    /// The dialogue tree ID to use (e.g. "meet_stranger", "strengthen_relationship").
    /// Null when the tree should be resolved at runtime based on current affinity.
    /// </summary>
    public string? TreeId { get; init; }

    public DialogueOutcome(NpcEntity target, string? treeId = null)
    {
        Target = target;
        TreeId = treeId;
    }

    /// <summary>No keywords — selection is driven by the GOAL LLM, not keyword clicking.</summary>
    public override List<KeywordInContext> OutcomeKeywordsInContext => new();

    public override string DisplayName => $"Talk to {Target.DisplayName}";

    public override string ToNaturalLanguageString()
        => $"engage in conversation with {Target.DisplayName}";
}
