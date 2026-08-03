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
///
/// <para>
/// <b>Authoring the neutral text.</b> No line in a tree is ever spoken as written. Every replica —
/// <see cref="NpcLineNode.Replica"/>, <see cref="PlayerOption.Replica"/> and both of a
/// <see cref="ResolutionNode"/>'s — is handed to a persona (the NPC's, or the modus mentis voicing
/// the reply) to be said in that persona's own words.
/// </para>
/// <para>
/// <b>Write the spoken words, plainly.</b> The replica is the line as it would leave a mouth — "Who
/// are you?" — and the persona re-says it in its own wording. So it carries content only: no dialect
/// or period colour ("aye", "naught", "hereabouts"), no metaphor, simile or imagery, no aphorism, no
/// ellipses, no exclamation marks, no rhetorical repetition. One or two short sentences. Write "No
/// one important. Someone travelling through." and let the persona produce "Nobody worth writing
/// down. Just someone on the road." — authoring the second is doing the persona's job for it, and
/// worse, the persona then ornaments the ornament.
/// </para>
/// <para>
/// <b>Each replica also carries an indirect-speech twin</b> (<c>ReplicaIndirect</c>, and on a
/// <see cref="ResolutionNode"/> one per outcome): the same line reported rather than spoken — "I ask
/// them who they are". Nothing reads it today. It exists because the rewrite prompt has been a
/// description-to-speech task once already, and turning ~470 lines from one form into the other by
/// hand is the expensive half of that change; keeping the pair makes it a switch rather than a
/// rewrite. Update it when you change the line it belongs to.
/// </para>
/// <para>
/// <b>The NPC's third form.</b> An <see cref="NpcLineNode"/> also carries <c>ReplicaHeard</c>: the
/// same line reported from the <i>player's</i> side ("{npc:name} asks me who I am"). This one has a
/// live consumer — the prompt that grades which reply to offer, which needs to know what was just
/// said. It is authored rather than derived from <c>ReplicaIndirect</c> because the two differ by
/// more than a pronoun swap: grammatical person, verb agreement and the referent of "me" all move.
/// Neither a <see cref="PlayerOption"/> nor a <see cref="ResolutionNode"/> has one: nothing is ever
/// told what the player said, and a resolution ends the conversation.
/// </para>
/// <para>
/// <b>Flavour tokens are speaker-side only.</b> <c>{npc:introduction}</c>, <c>{npc:labour}</c>,
/// <c>{npc:craft}</c>, <c>{npc:workplace}</c>, <c>{npc:job}</c> and every <c>{npc:opinion_*}</c>
/// expand to a first-person clause the NPC would utter — "weeding, hauling, mending, and starting
/// again where I left off yesterday". That "I" is the NPC, which is right in the spoken line and in a
/// report written from the NPC's side, and wrong in a <c>ReplicaHeard</c>, where it would read as the
/// listener describing their own day. A heard form therefore names the <i>subject</i> instead of
/// carrying the content: "{npc:name} tells me what their working day is like". Only noun-phrase
/// tokens — <c>{npc:name}</c>, <c>{you:name}</c>, <c>{npc:job_offer}</c>, <c>{npc:job_pay}</c>,
/// <c>{npc:sells}</c>, <c>{npc:buys}</c>, <c>{npc:wares}</c>, <c>{you:goods}</c> — are safe there.
/// </para>
/// <para>
/// What must survive the plainness: the speech act (a question stays a question, an offer an offer),
/// every fact a later node depends on, every <c>{scope:field}</c> token, and — at a resolution — an
/// unmistakable difference between the success and failure lines.
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
    /// The modus mentis a successful resolution of this tree teaches, or null to teach nothing beyond
    /// the experience the chosen replies already earn. Applied by the same known-vs-unknown rule
    /// verbs use — see <c>ModusMentisGrantOutcome</c>.
    ///
    /// <para>This is the conversation's own lesson, and it is deliberately separate from the lesson
    /// the <i>verb</i> teaches: walking up to someone and opening your mouth is social interaction
    /// whatever follows (<c>DialogueVerb.GrantedModusMentisId</c>), while successfully begging a
    /// stranger for a coin is beggary.</para>
    /// </summary>
    public virtual string? GrantedModusMentisId => null;

    /// <summary>
    /// Further lessons a successful resolution teaches, decided from the branch that was walked and
    /// the person who was talking. Empty for every tree whose reward is one fixed thing.
    ///
    /// <para>Exists for GATHER KNOWLEDGE, where what you learn depends on what you asked about and
    /// who you asked: the general skill of getting somebody to talk is the same every time, and the
    /// substance of what they told you is not.</para>
    /// </summary>
    public virtual IEnumerable<string> AdditionalGrantedModusMentisIds(NpcEntity npc, ResolutionNode resolution)
        => System.Array.Empty<string>();

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
