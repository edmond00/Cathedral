using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// A named dialogue tree — a directed graph of <see cref="DialogueTreeNode"/>s with a
/// fixed entry point and a guarding availability condition.
/// Each tree is associated with a verb that can trigger it.
/// Tree instances are stateless; all session state lives in the runtime controller.
/// </summary>
public abstract class DialogueTree
{
    /// <summary>Unique identifier (e.g. "meet_stranger").</summary>
    public abstract string TreeId { get; }

    /// <summary>Human-readable name shown in the verb list.</summary>
    public abstract string DisplayName { get; }

    /// <summary>
    /// Overall subject of this dialogue — used in NPC and modus mentis LLM prompts.
    /// e.g. "meeting a stranger for the first time"
    /// </summary>
    public abstract string Description { get; }

    /// <summary>The verb ID that triggers this tree (e.g. "meet_stranger").</summary>
    public abstract string AssociatedVerbId { get; }

    /// <summary>Entry node — first node shown to the player.</summary>
    public abstract DialogueTreeNode EntryNode { get; }

    /// <summary>
    /// Returns whether this tree can be started given the NPC's current affinity
    /// with the party member identified by <paramref name="partyMemberId"/>.
    /// </summary>
    public abstract bool IsAvailable(NpcEntity npc, string partyMemberId);
}
