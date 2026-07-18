using System;
using System.Collections.Generic;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// One step in a dialogue tree.
/// Intermediate nodes have Branches (possible next nodes chosen by the modus mentis).
/// Terminal nodes have Outcomes (effects applied when the dialogue resolves here).
/// A node must not have both Branches and Outcomes.
/// </summary>
public class DialogueTreeNode
{
    /// <summary>Unique identifier within the tree (e.g. "greeting", "salutation").</summary>
    public string NodeId { get; }

    /// <summary>
    /// Short description of the intent of this step, used as context in branch-selection LLM prompts
    /// and in debug tooling. NOT the spoken line — see <see cref="Replica"/>.
    /// e.g. "greeting the stranger for the first time"
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// The direct, short neutral spoken line for this step, written as first-person direct speech
    /// where "I" is the speaker and "you" is the person addressed (e.g. "And who might you be?").
    /// The same line reads naturally whether the NPC opens with it or the player replies with it.
    /// It is handed to the persona rewriter, which keeps its meaning and adds character flavour.
    /// </summary>
    public string Replica { get; }

    /// <summary>Whether this is the entry point of the tree.</summary>
    public bool IsEntry { get; }

    /// <summary>
    /// Possible next nodes — present for intermediate nodes.
    /// The modus mentis chooses among these. Empty for terminal nodes.
    /// </summary>
    public IReadOnlyList<DialogueBranch> Branches { get; }

    /// <summary>
    /// Outcomes applied when this node resolves — present for terminal nodes.
    /// Empty for intermediate nodes.
    /// </summary>
    public IReadOnlyList<DialogueOutcomeCase> Outcomes { get; }

    /// <summary>True when this node ends the dialogue (no branches).</summary>
    public bool IsTerminal => Branches.Count == 0;

    public DialogueTreeNode(
        string                     nodeId,
        string                     description,
        string                     replica  = "",
        bool                       isEntry  = false,
        List<DialogueBranch>?      branches = null,
        List<DialogueOutcomeCase>? outcomes = null)
    {
        if ((branches?.Count ?? 0) > 0 && (outcomes?.Count ?? 0) > 0)
            throw new ArgumentException($"DialogueTreeNode '{nodeId}': a node cannot have both Branches and Outcomes.");

        NodeId      = nodeId;
        Description = description;
        Replica     = replica;
        IsEntry     = isEntry;
        Branches    = branches ?? new List<DialogueBranch>();
        Outcomes    = outcomes ?? new List<DialogueOutcomeCase>();
    }
}
