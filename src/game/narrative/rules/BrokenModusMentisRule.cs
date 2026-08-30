namespace Cathedral.Game.Narrative.Rules;

/// <summary>
/// Blocks an action whose chosen modus mentis this body can no longer carry — wounds have dragged
/// its effective level to 0 or below, so there is not a single die left to roll with it.
/// See <see cref="BrokenModusMentis"/> for the rule and why it refuses rather than withholds.
///
/// <para><b>First in the list, ahead of every circumstantial rule</b>, because it is the only one
/// about the body rather than the situation. Told "that man is watching" when the truth is that
/// their arm is ruined, a player moves to another room and finds the same refusal waiting — the
/// order is what makes the news actionable.</para>
///
/// <para>The reason names the wounded part and the wounds responsible, and is a fragment so
/// <c>NeutralNarration.ActionImpossible</c> can frame it like any other refusal. It is then re-voiced
/// by the very modus mentis that failed, which is the point: the account of the ruined arm comes
/// from the disposition that wanted to use it.</para>
/// </summary>
public class BrokenModusMentisRule : IActionRule
{
    public ActionRuleResult Check(ActionRuleContext ctx)
    {
        var mm = ctx.ActionModusMentis;
        if (mm == null) return ActionRuleResult.Pass();
        if (!ctx.Actor.IsModusMentisBroken(mm)) return ActionRuleResult.Pass();

        return ActionRuleResult.Fail(BrokenModusMentis.ReasonFor(ctx.Actor, mm));
    }
}
