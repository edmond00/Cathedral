using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Npc;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative;

/// <summary>
/// A unified description of "what phase happens next", produced both by the normal narration flow
/// and by routine replay, and applied in a single place by the game controller
/// (<c>LocationTravelGameController.ApplyPhaseTransition</c>). Adding a future phase kind means
/// adding one subclass here and one switch arm in the controller — no new ad-hoc pending fields.
/// </summary>
public abstract class PhaseTransition { }

/// <summary>Return to the world-travel view (no further phase).</summary>
public sealed class ReturnToTravelTransition : PhaseTransition
{
    public static readonly ReturnToTravelTransition Instance = new();
}

/// <summary>
/// Start (or continue into) a narration phase. <see cref="StartArea"/> and <see cref="Time"/> are
/// optional hints used by routine replay so narration resumes at the area the routine ended in,
/// at the routine's recorded time period.
/// </summary>
public sealed class StartNarrationTransition : PhaseTransition
{
    public int Vertex { get; }
    public Area? StartArea { get; }
    public TimePeriod? Time { get; }

    public StartNarrationTransition(int vertex, Area? startArea = null, TimePeriod? time = null)
    {
        Vertex    = vertex;
        StartArea = startArea;
        Time      = time;
    }
}

/// <summary>Start a fight against an NPC.</summary>
public sealed class StartFightTransition : PhaseTransition
{
    public NpcEntity Enemy { get; }
    public string Reason { get; }

    /// <summary>When true the enemy has the initiative (surprise round) — see <see cref="FightOutcome.EnemyInitiative"/>.</summary>
    public bool EnemyInitiative { get; }

    public StartFightTransition(NpcEntity enemy, string reason, bool enemyInitiative = false)
    {
        Enemy  = enemy;
        Reason = reason;
        EnemyInitiative = enemyInitiative;
    }
}

/// <summary>Start a dialogue with an NPC (by registry tree id or a pre-built tree).</summary>
public sealed class StartDialogueTransition : PhaseTransition
{
    public NpcEntity Npc { get; }
    public string? TreeId { get; }
    public DialogueTree? Tree { get; }

    public StartDialogueTransition(NpcEntity npc, string? treeId = null, DialogueTree? tree = null)
    {
        Npc    = npc;
        TreeId = treeId;
        Tree   = tree;
    }
}
