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
    /// <summary>The neutral NPC line (direct speech; may contain <c>{scope:field}</c> template tokens).</summary>
    public string Replica { get; }

    /// <summary>The player replies offered after this line.</summary>
    public IReadOnlyList<PlayerOption> Options { get; }

    public NpcLineNode(string nodeId, string replica, params PlayerOption[] options)
        : base(nodeId)
    {
        Replica = replica;
        Options = options ?? System.Array.Empty<PlayerOption>();
    }

    public NpcLineNode(string nodeId, string replica, List<PlayerOption> options)
        : base(nodeId)
    {
        Replica = replica;
        Options = options ?? new List<PlayerOption>();
    }
}
