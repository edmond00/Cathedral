using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Narrative.Rules;

/// <summary>
/// Blocks illegal actions when a witness is standing right there (visual range)
/// and the chosen modus mentis is not amoral enough to act under observation.
///
/// A <see cref="MoralLevel.Low"/> modus mentis will act brazenly regardless.
/// <see cref="MoralLevel.Medium"/> and <see cref="MoralLevel.High"/> refuse to commit
/// a crime in plain sight — use a more deceptive modus mentis or wait for privacy.
/// (High morality is already blocked by <see cref="IllegalActionHighMoralityRule"/>
/// for any illegal action; this rule specifically handles the visual-witness context
/// for medium-morality modus mentis.)
/// </summary>
public class IllegalActionVisualWitnessRule : IActionRule
{
    public ActionRuleResult Check(ActionRuleContext ctx)
    {
        if (ctx.WitnessContext.Type != WitnessType.Visual) return ActionRuleResult.Pass();
        if (!IsIllegalAction(ctx))                         return ActionRuleResult.Pass();

        var mm = ctx.ActionModusMentis;
        if (mm == null || mm.MoralLevel == MoralLevel.Low) return ActionRuleResult.Pass();

        var witnessName = ctx.WitnessContext.Witness?.DisplayName ?? "someone";
        return ActionRuleResult.Fail(
            $"Your {mm.DisplayName} won't do this with {witnessName} watching.");
    }

    private static bool IsIllegalAction(ActionRuleContext ctx)
        => !ctx.Action.Verb.IsLegal || ctx.PoV?.Where.IsPrivate == true;
}
