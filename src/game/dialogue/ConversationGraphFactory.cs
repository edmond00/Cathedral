namespace Cathedral.Game.Dialogue;

/// <summary>
/// Abstract factory that creates and connects subject nodes for a specific NPC type.
/// Mirrors NarrationGraphFactory but for dialogue.
/// </summary>
public abstract class ConversationGraphFactory
{
    /// <summary>
    /// Create all subject nodes, wire their PossiblePositiveOutcomes and NegativeOutcome,
    /// then return the entry node.
    /// </summary>
    public ConversationSubjectNode CreateGraph()
    {
        var entry = BuildNodes();
        ConnectNodes();
        return entry;
    }

    /// <summary>
    /// Instantiate all nodes and set outcomes. Return the entry node.
    /// </summary>
    protected abstract ConversationSubjectNode BuildNodes();

    /// <summary>
    /// Wire Transitions between nodes after all nodes are created.
    /// </summary>
    protected abstract void ConnectNodes();
}
