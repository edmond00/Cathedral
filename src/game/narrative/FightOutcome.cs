using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Outcome that triggers combat with an NPC.
/// Can be a positive outcome (player chose to attack) or a failure outcome (NPC attacks player).
/// When applied, the narrative controller pauses and enters fight mode.
/// </summary>
public class FightOutcome : OutcomeBase
{
    /// <summary>The NPC to fight.</summary>
    public NpcEntity Target { get; }

    /// <summary>Context text used for arena theming and narration (e.g., "ambushed by a wolf in the clearing").</summary>
    public string CombatContext { get; }

    public FightOutcome(NpcEntity target, string combatContext = "")
    {
        Target = target;
        CombatContext = string.IsNullOrEmpty(combatContext)
            ? $"combat with {target.DisplayName}"
            : combatContext;
    }

    public override string DisplayName => $"Fight {Target.DisplayName}";

    public override string ToNaturalLanguageString()
        => $"engage in combat with {Target.DisplayName}";
}
