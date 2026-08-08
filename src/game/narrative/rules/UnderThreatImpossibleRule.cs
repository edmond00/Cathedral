using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.Rules;

/// <summary>
/// Blocks a non-combat action when an enemy is standing in the same area — right here, leaving no
/// room to do anything but face the threat.
///
/// <para>Two exemptions, and they are different in kind. A verb that is valid under threat
/// (<see cref="Cathedral.Game.Scene.Verbs.Verb.CanBeUsedUnderThreat"/>: attack, slay, appease,
/// reconcile) is never blocked, because facing the threat is what it does. A <b>discrete</b> modus
/// mentis is never blocked either, because acting under somebody's eyes is precisely what
/// discreteness is for — but it buys permission, not safety: failing in front of an enemy still
/// starts the fight (see <see cref="ProximityModel"/>).</para>
///
/// <para>Read from raw proximity. An enemy at Audio range never blocks an action; they only come
/// looking when one fails. The refusal reason is first-person for re-expression in the modus
/// mentis's voice.</para>
/// </summary>
public class UnderThreatImpossibleRule : IActionRule
{
    public ActionRuleResult Check(ActionRuleContext ctx)
    {
        if (ctx.Action.Verb.CanBeUsedUnderThreat) return ActionRuleResult.Pass();
        if (ctx.ActionModusMentis?.ActsDiscretely == true) return ActionRuleResult.Pass();
        if (ctx.ThreatContext.Level != ThreatLevel.Visual) return ActionRuleResult.Pass();

        var enemyName = ctx.ThreatContext.Threat?.DisplayName ?? "an enemy";
        return ActionRuleResult.Fail($"I cannot, while under the threat of {enemyName} right here.");
    }
}
