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
/// dice check resolves. The dice pool is the NPC affinity bonus, the summed levels of every
/// Modus Mentis that voiced a chosen reply along the branch, the speaker's beauty and their attire;
/// <see cref="Difficulty"/> is the fixed,
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

    // Both spoken lines are plain content, like every other replica (see "Authoring the neutral text"
    // on DialogueTree) — with one extra duty: since the persona re-says them in words of its own, the
    // difference between them has to live in what they say, not in how warmly they say it.
    //
    // Neither needs a "heard" counterpart: a resolution ends the conversation, so no later prompt is
    // ever told what was said here.

    /// <summary>What the NPC says when the check succeeds, as spoken words (may contain tokens).</summary>
    public string SuccessReplica { get; }

    /// <summary>What the NPC says when the check fails, as spoken words (may contain tokens).</summary>
    public string FailureReplica { get; }

    /// <summary>
    /// <see cref="SuccessReplica"/> as an indirect-speech report from the NPC's side. Unused at
    /// runtime; kept for the same reason as <see cref="NpcLineNode.ReplicaIndirect"/>.
    /// </summary>
    public string SuccessReplicaIndirect { get; }

    /// <summary><see cref="FailureReplica"/> as an indirect-speech report. Unused at runtime.</summary>
    public string FailureReplicaIndirect { get; }

    /// <summary>Whether this node rolls the dice or forces a fixed result.</summary>
    public ResolutionMode Mode { get; }

    public ResolutionNode(
        string         nodeId,
        int            difficulty,
        string         successReplica,
        string         successReplicaIndirect,
        string         failureReplica,
        string         failureReplicaIndirect,
        ResolutionMode mode = ResolutionMode.DiceCheck,
        string?        topic = null,
        params System.Type[] lessons)
        : base(nodeId)
    {
        Topic                  = topic;
        Lessons                = lessons ?? System.Array.Empty<System.Type>();
        Difficulty             = System.Math.Max(1, difficulty);
        SuccessReplica         = successReplica;
        SuccessReplicaIndirect = successReplicaIndirect;
        FailureReplica         = failureReplica;
        FailureReplicaIndirect = failureReplicaIndirect;
        Mode                   = mode;
    }

    /// <summary>
    /// What this branch was about, for trees whose lesson depends on the subject rather than on the
    /// tree. Null for every tree where succeeding teaches one fixed thing, which is most of them.
    ///
    /// <para>Only GATHER KNOWLEDGE uses it so far: asking a blacksmith about his trade should teach
    /// metalcraft and asking him about the village should teach the lie of the land, and the tree
    /// cannot know which branch was walked without being told.</para>
    /// </summary>
    public string? Topic { get; }

    /// <summary>
    /// What <b>this branch</b> teaches, as modus mentis types — <c>typeof(CondolenceModusMentis)</c>.
    ///
    /// <para>The branch is the unit that knows what a conversation was about, and until this existed
    /// nothing could say so: a tree could only infer it from <see cref="Difficulty"/>, which is
    /// derived from depth by a ladder that produces 1, 2 or 3. Fifty-eight authored endings of
    /// STRENGTHEN RELATIONSHIP collapsed into two buckets, so a branch about a dead wife and one
    /// about the harvest taught the same thing.</para>
    ///
    /// <para>Types rather than ids, so a lesson that does not exist fails to compile. Empty on every
    /// branch nobody has written a lesson for, which falls through to whatever the tree grants.</para>
    /// </summary>
    public System.Collections.Generic.IReadOnlyList<System.Type> Lessons { get; }
}
