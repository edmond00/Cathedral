using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.Rules;

/// <summary>
/// Blocks a non-combat action when an enemy is at <b>effective-Visual</b> proximity — right here,
/// leaving no room to do anything but face the threat. Verbs that are valid under threat
/// (<see cref="Cathedral.Game.Scene.Verbs.Verb.CanBeUsedUnderThreat"/>: attack, slay, appease,
/// reconcile) are never blocked.
///
/// Discreteness downgrades the threat's proximity by one step (see <see cref="ProximityModel"/>): a
/// discrete modus mentis can act under a same-area enemy (risking a fight only on failure), while a
/// non-discrete modus mentis is blocked. Audio-range enemies never block the action (they only risk a
/// fight on failure). The refusal reason is first-person for re-expression in the modus mentis's voice.
/// </summary>
public class UnderThreatImpossibleRule : IActionRule
{
    public ActionRuleResult Check(ActionRuleContext ctx)
    {
        if (ctx.Action.Verb.CanBeUsedUnderThreat) return ActionRuleResult.Pass();

        bool discrete = ctx.ActionModusMentis?.ActsDiscretely ?? false;
        if (ProximityModel.Effective(ctx.ThreatContext.Level, discrete) != ThreatLevel.Visual)
            return ActionRuleResult.Pass();

        var enemyName = ctx.ThreatContext.Threat?.DisplayName ?? "an enemy";
        return ActionRuleResult.Fail($"I cannot, while under the threat of {enemyName} right here.");
    }
}
