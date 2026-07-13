using System.Collections.Generic;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Outcome that triggers combat with an NPC.
/// Can be a positive outcome (player chose to attack) or a failure outcome (NPC attacks player).
/// When applied, the narrative controller pauses and enters fight mode.
/// Extends ConcreteOutcome so it can appear as a sub-outcome inside NpcObservationObject.
/// </summary>
public class FightOutcome : ConcreteOutcome
{
    /// <summary>The NPC to fight.</summary>
    public NpcEntity Target { get; }

    /// <summary>Context text used for arena theming and narration (e.g., "ambushed by a wolf in the clearing").</summary>
    public string CombatContext { get; }

    /// <summary>
    /// When true the enemy has the initiative (a surprise round): enemy fighters act before the party
    /// regardless of the initiative roll. Set for fights that start because an action failed under
    /// threat; false for player-initiated attacks.
    /// </summary>
    public bool EnemyInitiative { get; init; }

    /// <summary>
    /// Contextual label substituted for the proper name in the LLM-facing goal phrase.
    /// Null until stamped by <see cref="Npc.NpcObservationObject"/>; the human-facing
    /// <see cref="DisplayName"/> and <see cref="CombatContext"/> always keep the real name.
    /// </summary>
    public string? ContextLabel { get; set; }

    public FightOutcome(NpcEntity target, string combatContext = "")
    {
        Target = target;
        CombatContext = string.IsNullOrEmpty(combatContext)
            ? $"combat with {target.DisplayName}"
            : combatContext;
    }

    public override string DisplayName => $"Fight {Target.DisplayName}";

    public override string ToNaturalLanguageString()
        => $"engage in combat with {ContextLabel ?? Target.DisplayName}";

}
