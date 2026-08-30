using Cathedral.Game.Narrative;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// The explicit statement that a conversation ended and changed nothing.
///
/// <para>The one <see cref="Outcome"/> that is a message rather than a consequence: it has no
/// <c>Apply</c>, because there is nothing to carry out. It exists because a branch can legitimately
/// resolve with no outcomes at all — <c>ProposeToBuyTree</c>, <c>ProposeToSellTree</c> and
/// <c>RequestJobTree</c> declare no failure consequences, so failing to strike a bargain or to be
/// taken on costs nothing — and an outcome block with no chips in it reads as a bug rather than as
/// "nothing happened".</para>
///
/// <para>Named for that single job. It was <c>DialogueOutcomeReport</c>, a general-sounding name for
/// the generic chip every conversation effect went through, back when those effects could not word
/// themselves. They can now (see <see cref="Outcome.Report"/>), so the only thing left that needs a
/// chip with no effect behind it is this.</para>
/// </summary>
public sealed class NoDialogueConsequenceOutcome : Outcome
{
    // Rendered in the dialogue panel, never listed in an action's outcome sentence, so it carries no
    // narration verbatim.
    public NoDialogueConsequenceOutcome(string npcName)
        : base($"Nothing changes between you and {npcName}",
               OutcomeSeverity.Neutral, verbatim: string.Empty) { }
}
