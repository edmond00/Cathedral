namespace Cathedral.Game.Dialogue;

/// <summary>
/// Base class for all conversation outcomes.
/// </summary>
public abstract class ConversationOutcome
{
    /// <summary>Natural-language hint for the LLM describing what this outcome aims for.</summary>
    public abstract string OutcomeHint { get; }
}

/// <summary>
/// Positive outcome that transitions the conversation to a new subject node.
/// </summary>
public class NodeTransitionOutcome : ConversationOutcome
{
    public ConversationSubjectNode TargetNode { get; }
    public override string OutcomeHint => $"lead the conversation toward: {TargetNode.ContextDescription}";

    public NodeTransitionOutcome(ConversationSubjectNode targetNode)
    {
        TargetNode = targetNode;
    }
}

/// <summary>
/// Outcome that modifies the NPC's affinity with the protagonist.
/// Positive delta = warmer attitude; negative delta = cold or hostile.
/// </summary>
public class AffinityOutcome : ConversationOutcome
{
    public float AffinityDelta { get; }
    public override string OutcomeHint => AffinityDelta >= 0
        ? "make the NPC feel positively toward you"
        : "the NPC reacts poorly to your approach";

    public AffinityOutcome(float delta)
    {
        AffinityDelta = delta;
    }
}
