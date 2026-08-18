using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Narrative.Rules;

/// <summary>
/// Blocks a blow a body has nothing to throw with. The counterpart of <see cref="RequiredToolRule"/>
/// for the one verb that is struck with a fighting medium rather than worked at with an implement:
/// where that rule catches a seam attacked with fingernails, this catches a fight opened by somebody
/// with no working limb left, or with a weapon whose kind knows no attack.
///
/// <para><b>It exists because the alternative failure is silent.</b> Without it the action rolls,
/// succeeds, and produces no blow — <c>FirstBlowOutcome.For</c> returns null and the fight opens with
/// nobody touched — which reads as the verb having quietly done nothing, at the cost of a noetic
/// point and a die roll. Deterministic and absolute, like every rule here: no amount of resolve puts
/// a punch in an arm ruined at the shoulder.</para>
///
/// <para>Reachable in two ways, and both are ordinary play rather than edge cases: a body worn down
/// by High-handicap wounds (which is also what takes the medium out of use inside a fight), and an
/// implement of a weapon kind carrying nothing but guards — a shield knows Cover, Parry and one
/// bash, and a kind that lost its bash would offer no opening at all.</para>
/// </summary>
public class FirstBlowMediumRule : IActionRule
{
    public ActionRuleResult Check(ActionRuleContext ctx)
    {
        var verb = ctx.Action.PreselectedOutcome?.Verb;
        if (verb is not { UsesFightingMedium: true }) return ActionRuleResult.Pass();

        var refusal = FirstBlow.Refusal(ctx.Actor, ctx.Action.CombinedItem);
        return refusal == null ? ActionRuleResult.Pass() : ActionRuleResult.Fail(refusal);
    }
}
