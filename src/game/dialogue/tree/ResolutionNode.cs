namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// How a <see cref="ResolutionNode"/> decides success vs failure.
/// </summary>
public enum ResolutionMode
{
    /// <summary>Roll the accumulated dice pool against the node's difficulty (the normal case).</summary>
    DiceCheck,

    /// <summary>Force the tree's success outcome with no dice roll.</summary>
    ForceSuccess,

    /// <summary>Force the tree's failure outcome with no dice roll.</summary>
    ForceFailure,
}

/// <summary>
/// The branch end: reached after the player's last reply, this is where the conversation's single
/// dice check resolves. The dice pool is the NPC affinity bonus plus the summed levels of every
/// Modus Mentis that voiced a chosen reply along the branch; <see cref="Difficulty"/> is the fixed,
/// authored number of 6s needed to succeed, clamped at roll time to the pool size so it is always
/// reachable.
///
/// <para>
/// After the roll the NPC speaks one of two lines — <see cref="SuccessReplica"/> or
/// <see cref="FailureReplica"/> — and the owning tree's <see cref="DialogueTree.SuccessOutcomes"/>
/// or <see cref="DialogueTree.FailureOutcomes"/> fire. A node may instead force its result with no
/// roll (see <see cref="ResolutionMode"/>): a <see cref="ResolutionMode.ForceFailure"/> node, for
/// example, guarantees the failure outcome (used by the caught-red-handed "provoke" branch).
/// </para>
/// </summary>
public class ResolutionNode : DialogueNode
{
    /// <summary>Fixed number of 6s the pool must roll to succeed (authored per node). Ignored when <see cref="Mode"/> is forced.</summary>
    public int Difficulty { get; }

    /// <summary>NPC line spoken when the check succeeds (neutral; may contain template tokens).</summary>
    public string SuccessReplica { get; }

    /// <summary>NPC line spoken when the check fails (neutral; may contain template tokens).</summary>
    public string FailureReplica { get; }

    /// <summary>Whether this node rolls the dice or forces a fixed result.</summary>
    public ResolutionMode Mode { get; }

    public ResolutionNode(
        string         nodeId,
        int            difficulty,
        string         successReplica,
        string         failureReplica,
        ResolutionMode mode = ResolutionMode.DiceCheck)
        : base(nodeId)
    {
        Difficulty     = System.Math.Max(1, difficulty);
        SuccessReplica = successReplica;
        FailureReplica = failureReplica;
        Mode           = mode;
    }
}
