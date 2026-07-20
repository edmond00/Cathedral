namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// Base type for a node in a dialogue tree. Nodes are <b>speaker-typed</b>: an
/// <see cref="NpcLineNode"/> is spoken by the NPC and a <see cref="ResolutionNode"/> is the
/// branch-end where the single dice check resolves. Player lines are not nodes — they are
/// <see cref="PlayerOption"/> edges hanging off an <see cref="NpcLineNode"/>.
///
/// <para>
/// Unlike the old model there are no shared lines: every node and option belongs to exactly one
/// speaker, so the flow reads as a real two-person conversation and is trivial to colour in the
/// debug viewer.
/// </para>
/// </summary>
public abstract class DialogueNode
{
    /// <summary>Identifier, unique within its tree (e.g. "greeting", "ask_who", "farewell").</summary>
    public string NodeId { get; }

    protected DialogueNode(string nodeId) => NodeId = nodeId;
}
