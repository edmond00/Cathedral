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

    public ActionRuleResult Check(ActionRuleContext ctx)
    {
        var vo = ctx.Action.PreselectedOutcome;
        // Every verb that opens a conversation derives from DialogueVerb, so ask the type rather
        // than keeping a list of ids. The list this replaces was already missing three verbs by the
        // time anyone looked at it, and a dialogue verb missing from it lets a character with no
        // tongue hold a conversation.
        if (vo.VerbView.Verb is not Cathedral.Game.Scene.Verbs.DialogueVerb) return ActionRuleResult.Pass();
        if (DialogueOptionGenerator.SpeechFluency(ctx.Actor) > 0) return ActionRuleResult.Pass();

        return ActionRuleResult.Fail(
            "No words come — the tongue cannot shape a single reply.");
    }
}
