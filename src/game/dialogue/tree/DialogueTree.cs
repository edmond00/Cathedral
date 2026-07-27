using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// A named dialogue tree — a directed graph of speaker-typed <see cref="DialogueNode"/>s
/// (<see cref="NpcLineNode"/> / <see cref="ResolutionNode"/>) with a fixed entry point and a
/// guarding availability condition. Each tree is associated with a verb that can trigger it.
/// Tree instances are stateless; all session state lives in the runtime controller.
///
/// <para>
/// A tree has exactly two outcome sets — <see cref="SuccessOutcomes"/> and
/// <see cref="FailureOutcomes"/> — shared by every branch. Which one fires is decided by the single
/// skill check at the branch-end <see cref="ResolutionNode"/> (or forced with no roll when the node
/// is a <see cref="ResolutionMode.ForceSuccess"/>/<see cref="ResolutionMode.ForceFailure"/> node).
/// </para>
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

    /// <summary>Entry node — the NPC's opening line and its player replies.</summary>
    public abstract NpcLineNode EntryNode { get; }

    /// <summary>Effects applied when the tree's single skill check succeeds (shared by all branches).</summary>
    public abstract IReadOnlyList<IDialogueOutcome> SuccessOutcomes { get; }

    /// <summary>Effects applied when the tree's single skill check fails (shared by all branches).</summary>
    public abstract IReadOnlyList<IDialogueOutcome> FailureOutcomes { get; }

    /// <summary>
    /// How a successful trigger of this dialogue interacts with routine recording/replay.
    /// Default: <see cref="DialogueRoutineBehavior.Interrupt"/> (recording stops before the dialogue).
    /// </summary>
    public virtual DialogueRoutineBehavior RoutineBehavior => DialogueRoutineBehavior.Interrupt;

    /// <summary>
    /// Returns whether this tree can be started given the NPC's current affinity
    /// with the party member identified by <paramref name="partyMemberId"/>.
    /// </summary>
    public abstract bool IsAvailable(NpcEntity npc, string partyMemberId);
}

/// <summary>
/// How a dialogue tree participates in routine recording when a recordable verb triggers it.
/// </summary>
public enum DialogueRoutineBehavior
{
    /// <summary>Recording stops before the dialogue; the dialogue is never part of the routine.</summary>
    Interrupt,

    /// <summary>
    /// The dialogue-trigger step is recorded. Replaying the routine starts the dialogue directly;
    /// the dialogue's success is not recorded (it is rolled live each replay).
    /// </summary>
    IncludeTrigger,

    /// <summary>
    /// The dialogue's success is baked into the routine. Replaying skips the dialogue and opens the
    /// follow-on phase (trade / work menu) directly.
    /// </summary>
    IncludeSuccess,
}
