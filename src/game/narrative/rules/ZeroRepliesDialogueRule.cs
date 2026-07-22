using Cathedral.Game.Dialogue.Runtime;

namespace Cathedral.Game.Narrative.Rules;

/// <summary>
/// Blocks every dialogue-triggering verb when the actor's speech fluency is 0 — a tongue at
/// level 0 (or wound-disabled) can shape no reply at all, so any action that would open a
/// dialogue tree is impossible before it starts.
///
/// The verb list mirrors the verbs that return a <c>DialogueTriggerOutcome</c>
/// (see <c>src\game\scene\verbs</c>).
/// </summary>
public class ZeroRepliesDialogueRule : IActionRule
{
    private static readonly System.Collections.Generic.HashSet<string> DialogueVerbIds =
        new()
        {
            "meet_stranger",
            "strengthen_relationship",
            "reconcile",
            "request_job",
            "propose_to_sell",
            "propose_to_buy",
        };

    public ActionRuleResult Check(ActionRuleContext ctx)
    {
        var vo = ctx.Action.PreselectedOutcome;
        if (!DialogueVerbIds.Contains(vo.VerbView.Verb.VerbId)) return ActionRuleResult.Pass();
        if (DialogueOptionGenerator.SpeechFluency(ctx.Actor) > 0) return ActionRuleResult.Pass();

        return ActionRuleResult.Fail(
            "No words come — the tongue cannot shape a single reply.");
    }
}
