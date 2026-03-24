using System.Collections.Generic;

namespace Cathedral.Game.Dialogue;

/// <summary>
/// Represents a conversation subject the player can explore with an NPC.
/// Similar in spirit to NarrationNode, but for dialogue rather than exploration.
/// </summary>
public abstract class ConversationSubjectNode
{
    /// <summary>Unique identifier for this subject (e.g., "greeting", "rumors").</summary>
    public abstract string SubjectId { get; }

    /// <summary>Short description shown in the UI header (e.g., "Asking about local rumors").</summary>
    public abstract string ContextDescription { get; }

    /// <summary>Whether this node can be used as the entry point of a conversation.</summary>
    public virtual bool IsEntryNode => false;

    /// <summary>
    /// Base difficulty (0.0 = trivial, 1.0 = very hard) for skill checks in this subject.
    /// Modified at runtime by NPC affinity.
    /// </summary>
    public abstract float BaseDifficultyScore { get; }

    /// <summary>
    /// Possible positive outcomes when a skill check succeeds.
    /// Each maps to a different replica / outcome pair.
    /// Multiple to allow variety when multiple replicas are presented.
    /// </summary>
    public List<ConversationOutcome> PossiblePositiveOutcomes { get; set; } = new();

    /// <summary>
    /// The outcome applied on a failed skill check (always an affinity penalty).
    /// </summary>
    public AffinityOutcome NegativeOutcome { get; set; } = new AffinityOutcome(-10f);

    /// <summary>
    /// Nodes reachable via <see cref="NodeTransitionOutcome"/> from this subject.
    /// Populated by the factory's ConnectNodes step.
    /// </summary>
    public List<ConversationSubjectNode> Transitions { get; set; } = new();
}
