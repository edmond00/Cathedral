using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Base for the verbs that trigger a dialogue tree (meet, talk, propose-to-buy/sell, request-job, …).
/// Concrete verbs keep their own <c>IsPossible</c> / <c>Verbatim</c> / <c>SuccessReports</c> and only
/// supply <see cref="DialogueTreeId"/>.
/// </summary>
public abstract class DialogueVerb : Verb
{
    /// <summary>The id of the dialogue tree this verb triggers (e.g. "meet_stranger").</summary>
    protected abstract string DialogueTreeId { get; }

    /// <summary>
    /// Every dialogue verb needs language, so the whole family is declared here once rather than on
    /// each of the twelve. A beast has voice but not speech: it can howl, snarl and be understood as
    /// an animal is understood, and it can never open a tree.
    /// </summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;

    /// <summary>
    /// No implement bears on speech, so the whole family is excluded here rather than twelve times
    /// over. The roll only <i>opens</i> the conversation — everything that follows is the tree's, and
    /// a tree cannot see what was in the speaker's hand. Lending dice to "may I be presented to your
    /// master" for holding an axe is the clearest nonsense the combination could produce.
    /// </summary>
    public override ToolUsage ToolUse => ToolUsage.Excluded;

    /// <summary>
    /// Approaching someone and opening your mouth is the same skill whatever you then say, so every
    /// dialogue verb teaches social interaction. What the <i>conversation</i> teaches is the tree's
    /// business — see <see cref="DialogueTree.GrantedModusMentisId"/>, applied at the resolution.
    /// </summary>
    public override string? GrantedModusMentisId(Element? target) => "social_interaction";
}
