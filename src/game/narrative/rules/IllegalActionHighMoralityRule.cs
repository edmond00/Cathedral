using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Narrative.Rules;

/// <summary>
/// Blocks illegal actions when the chosen action modus mentis has <see cref="MoralLevel.High"/>.
/// A principled modus mentis refuses to participate in theft, trespass, or violence — even
/// if the player selects it. The player must switch to a less scrupulous modus mentis.
/// </summary>
public class IllegalActionHighMoralityRule : IActionRule
{
    public ActionRuleResult Check(ActionRuleContext ctx)
    {
        if (!IsIllegalAction(ctx)) return ActionRuleResult.Pass();

        var mm = ctx.ActionModusMentis;
        if (mm == null || mm.MoralLevel != MoralLevel.High) return ActionRuleResult.Pass();

        return ActionRuleResult.Fail(
            $"Your {mm.DisplayName} recoils at the idea — this goes against every principle it holds.");
    }

    private static bool IsIllegalAction(ActionRuleContext ctx)
        => !ctx.Action.Verb.IsLegal || ctx.PoV?.Where.IsPrivate == true;
}
