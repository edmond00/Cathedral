using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Trade;
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

    /// <summary>When true the enemy has the initiative (surprise round) — see <see cref="FightTriggerOutcome.EnemyInitiative"/>.</summary>
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

// ── Routine-replay bridges ──────────────────────────────────────────────────
// These are produced by a headless routine replay that ends on a dialogue-trigger step. Unlike the
// transitions above (raised from inside a live narration), they carry the location + a stable NPC
// key so the controller can rebuild narrative context and re-resolve the NPC before opening the
// sub-phase — see LocationTravelGameController.ApplyPhaseTransition.

/// <summary>Replay ended on an IncludeTrigger dialogue: rebuild context and open the dialogue directly.</summary>
public sealed class StartRoutineDialogueTransition : PhaseTransition
{
    public int Vertex { get; }
    public string NpcKey { get; }
    public string TreeId { get; }
    public TimePeriod Time { get; }

    /// <summary>Where the replay ended — see <see cref="StartRoutineTradeTransition.StartArea"/>.</summary>
    public Area? StartArea { get; }

    public StartRoutineDialogueTransition(int vertex, string npcKey, string treeId, TimePeriod time,
                                          Area? startArea = null)
    {
        Vertex    = vertex;
        NpcKey    = npcKey;
        TreeId    = treeId;
        Time      = time;
        StartArea = startArea;
    }
}

/// <summary>Replay ended on an IncludeSuccess trade dialogue: rebuild context and open the trade menu.</summary>
public sealed class StartRoutineTradeTransition : PhaseTransition
{
    public int Vertex { get; }
    public string NpcKey { get; }
    public TradeMode Mode { get; }
    public TimePeriod Time { get; }

    /// <summary>
    /// The area the replay ended in — the forge, not the square it was entered from.
    ///
    /// <para>These three bridges carried <see cref="Time"/> and not the area, while
    /// <see cref="StartNarrationTransition"/> carried both. So a routine that walked into a workshop
    /// and traded there put the player back on the scene's <i>default</i> opening area the moment the
    /// trade menu closed: the walk had happened headlessly and nothing told the rebuilt scene where
    /// it had ended. Matched by <c>ReferenceLemma</c> like the narration case, because the sub-phase
    /// rebuilds the scene and the replay's own <see cref="Area"/> instance belongs to a scene that no
    /// longer exists.</para>
    /// </summary>
    public Area? StartArea { get; }

    public StartRoutineTradeTransition(int vertex, string npcKey, TradeMode mode, TimePeriod time,
                                       Area? startArea = null)
    {
        Vertex    = vertex;
        NpcKey    = npcKey;
        Mode      = mode;
        Time      = time;
        StartArea = startArea;
    }
}

/// <summary>Replay ended on an IncludeSuccess request-job dialogue: rebuild context and open the work menu.</summary>
public sealed class StartRoutineWorkTransition : PhaseTransition
{
    public int Vertex { get; }
    public string NpcKey { get; }
    public string JobId { get; }
    public TimePeriod Time { get; }

    /// <summary>Where the replay ended — see <see cref="StartRoutineTradeTransition.StartArea"/>.</summary>
    public Area? StartArea { get; }

    public StartRoutineWorkTransition(int vertex, string npcKey, string jobId, TimePeriod time,
                                      Area? startArea = null)
    {
        Vertex    = vertex;
        NpcKey    = npcKey;
        JobId     = jobId;
        Time      = time;
        StartArea = startArea;
    }
}
