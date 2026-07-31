using System.Collections.Generic;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// A node where the NPC speaks a line, then offers the player a set of <see cref="PlayerOption"/>
/// replies to choose among. Used both as a tree's entry node and as the NPC's mid-conversation
/// responses. A well-formed <see cref="NpcLineNode"/> always has at least one option (a branch always
/// ends at a <see cref="ResolutionNode"/>, never at a bare NPC line).
/// </summary>
public class NpcLineNode : DialogueNode
{
    /// <summary>
    /// What the NPC says, as plain spoken words — "Good day. I do not think we have met before." This
    /// is the line the NPC's persona re-says in its own voice, so it carries content only. See
    /// "Authoring the neutral text" on <see cref="DialogueTree"/>.
    /// </summary>
    public string Replica { get; }

    /// <summary>
    /// The same line as an indirect-speech report from the NPC's own side — "I greet them and say I do
    /// not think we have met before". Not used at runtime today: the rewrite prompt is given
    /// <see cref="Replica"/>. It is kept because this and <see cref="Replica"/> have swapped roles
    /// before (the rewrite was briefly a description-to-speech task), and re-deriving one from the
    /// other by hand is the expensive half of that change.
    /// </summary>
    public string ReplicaIndirect { get; }

    /// <summary>
    /// The same line reported from the <b>listener's</b> side — "{npc:name} greets me and says they do
    /// not think we have met before". This is the one report with a live consumer: the prompt that
    /// grades which player reply to offer is told what was just said to the player. It is authored
    /// rather than derived from <see cref="ReplicaIndirect"/> because the two differ by more than a
    /// pronoun swap — grammatical person, verb agreement and the referent of "me" all move.
    /// </summary>
    public string ReplicaHeard { get; }

    /// <summary>The player replies offered after this line.</summary>
    public IReadOnlyList<PlayerOption> Options { get; }

    public NpcLineNode(string nodeId, string replica, string replicaIndirect, string replicaHeard,
                       params PlayerOption[] options)
        : base(nodeId)
    {
        Replica         = replica;
        ReplicaIndirect = replicaIndirect;
        ReplicaHeard    = replicaHeard;
        Options         = options ?? System.Array.Empty<PlayerOption>();
    }

    public NpcLineNode(string nodeId, string replica, string replicaIndirect, string replicaHeard,
                       List<PlayerOption> options)
        : base(nodeId)
    {
        Replica         = replica;
        ReplicaIndirect = replicaIndirect;
        ReplicaHeard    = replicaHeard;
        Options         = options ?? new List<PlayerOption>();
    }
}
