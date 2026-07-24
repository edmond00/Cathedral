using System.Collections.Generic;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// The branch end: reached after the player's last reply, this is where the conversation's single
/// dice check resolves. The dice pool is the NPC affinity bonus plus the summed levels of every
/// Modus Mentis that voiced a chosen reply along the branch; <see cref="Difficulty"/> is the fixed,
/// authored number of 6s needed to succeed, clamped at roll time to the pool size so it is always
/// reachable.
///
/// <para>
/// After the roll the NPC speaks one of two lines — <see cref="SuccessReplica"/> or
/// <see cref="FailureReplica"/> — and the matching <see cref="Outcomes"/> fire. The two lines are the
/// only place a node carries speaker-conditional text, and they are still the NPC's own lines (no
/// sharing with the player).
/// </para>
/// </summary>
public class ResolutionNode : DialogueNode
{
    /// <summary>Fixed number of 6s the pool must roll to succeed (authored per node).</summary>
    public int Difficulty { get; }

    /// <summary>NPC line spoken when the check succeeds (neutral; may contain template tokens).</summary>
    public string SuccessReplica { get; }

    /// <summary>NPC line spoken when the check fails (neutral; may contain template tokens).</summary>
    public string FailureReplica { get; }

    /// <summary>Effects applied on resolution, each gated by a <see cref="BranchCondition"/>.</summary>
    public IReadOnlyList<DialogueOutcomeCase> Outcomes { get; }

    public ResolutionNode(
        string                     nodeId,
        int                        difficulty,
        string                     successReplica,
        string                     failureReplica,
        List<DialogueOutcomeCase>? outcomes = null)
        : base(nodeId)
    {
        Difficulty     = System.Math.Max(1, difficulty);
        SuccessReplica = successReplica;
        FailureReplica = failureReplica;
        Outcomes       = outcomes ?? new List<DialogueOutcomeCase>();
    }
}
