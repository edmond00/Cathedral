using System.Collections.Generic;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Outcome that triggers a dialogue with an NPC.
/// Typically a positive outcome when peacefully approaching a dialogue-capable NPC.
/// When applied, the narrative controller pauses and enters dialogue mode.
/// Extends ConcreteOutcome so it can appear as a sub-outcome inside NpcObservationObject.
/// </summary>
public class DialogueOutcome : ConcreteOutcome
{
    /// <summary>The NPC to talk to.</summary>
    public NpcEntity Target { get; }

    public DialogueOutcome(NpcEntity target)
    {
        Target = target;
    }

    /// <summary>No keywords — selection is driven by the GOAL LLM, not keyword clicking.</summary>
    public override List<KeywordInContext> OutcomeKeywordsInContext => new();

    public override string DisplayName => $"Talk to {Target.DisplayName}";

    public override string ToNaturalLanguageString()
        => $"engage in conversation with {Target.DisplayName}";
}
