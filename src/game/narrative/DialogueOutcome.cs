using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Outcome that triggers a dialogue with an NPC.
/// Typically a positive outcome when peacefully approaching a dialogue-capable NPC.
/// When applied, the narrative controller pauses and enters dialogue mode.
/// </summary>
public class DialogueOutcome : OutcomeBase
{
    /// <summary>The NPC to talk to.</summary>
    public NpcEntity Target { get; }

    public DialogueOutcome(NpcEntity target)
    {
        Target = target;
    }

    public override string DisplayName => $"Talk to {Target.DisplayName}";

    public override string ToNaturalLanguageString()
        => $"engage in conversation with {Target.DisplayName}";
}
