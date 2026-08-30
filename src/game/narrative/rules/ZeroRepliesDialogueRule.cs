using System.Linq;
using Cathedral.Game.Dialogue.Runtime;

namespace Cathedral.Game.Narrative.Rules;

/// <summary>
/// Blocks every dialogue-triggering verb when the actor has nothing left to hold a conversation
/// with. Two ways that happens, and they are different pieces of news:
///
/// <list type="bullet">
///   <item><b>No voice</b> — speech fluency 0, a tongue at level 0 or wound-disabled, so no reply
///         can be shaped at all;</item>
///   <item><b>No means</b> — every speaking modus mentis the body holds has been broken by wounds
///         (see <see cref="BrokenModusMentis"/>). The tongue works; there is no disposition left
///         able to use it.</item>
/// </list>
///
/// <para><b>This is where dropping a broken speaking modus mentis surfaces.</b> Speech is the one
/// faculty that filters rather than refusing, because a dialogue reply has no narration frame to
/// carry a refusal — so the filtering is silent right up to the point it empties the list, and this
/// rule is what makes emptying it legible. Without it the conversation opens, generates no options
/// and dies with nothing said.</para>
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
        if (vo.Verb is not Cathedral.Game.Scene.Verbs.DialogueVerb) return ActionRuleResult.Pass();

        if (DialogueOptionGenerator.SpeechFluency(ctx.Actor) <= 0)
            return ActionRuleResult.Fail(
                "No words come — the tongue cannot shape a single reply.");

        // The body may still speak, but nothing it holds can carry a conversation any more. Name the
        // wounds, the way every other broken-modus-mentis refusal does: told only that they have
        // nothing to say, a player has no way to connect it to the injury that caused it.
        var usable = ctx.Actor.GetUsableSpeakingModiMentis();
        if (usable.Count > 0) return ActionRuleResult.Pass();

        var broken = ctx.Actor.GetSpeakingModiMentis();
        if (broken.Count == 0)
            return ActionRuleResult.Fail(
                "I have no turn of mind left that knows how to address anyone.");

        // Every speaking modus mentis is broken, so any of them accounts for it; take the one whose
        // body is worst off, which is the wound the player most needs to hear about.
        var worst = broken
            .OrderBy(mm => ctx.Actor.GetEffectiveModusMentisLevel(mm))
            .First();

        return ActionRuleResult.Fail(BrokenModusMentis.ReasonFor(ctx.Actor, worst));
    }
}
