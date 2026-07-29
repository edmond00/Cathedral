namespace Cathedral.Game.Narrative.Rules;

/// <summary>
/// Blocks a pickup action (grab/gather/steal/cut) when the acting member cannot take the item —
/// because it would exceed their carrying weight, because a liquid has no vessel to go in, or
/// because there is simply nowhere left to put it. Deterministic and absolute — checked before the
/// LLM critic. Pickup verbs report the item they would acquire via <c>Verb.AcquiredItem</c>;
/// non-pickup actions pass unconditionally.
///
/// The reason comes from <c>PartyMember.CanAcquire</c> already phrased in the first person, so the
/// refusal reads as the character's own thought once <c>NarrateRefusalAsync</c> re-voices it in the
/// acting modus mentis. A failed pickup is always explained, never silently dropped.
/// </summary>
public class InventoryCapacityRule : IActionRule
{
    public ActionRuleResult Check(ActionRuleContext ctx)
    {
        var outcome = ctx.Action.PreselectedOutcome;
        var item = outcome?.VerbView.Verb.AcquiredItem(outcome.Target);
        if (item == null) return ActionRuleResult.Pass();          // not a pickup

        var check = ctx.Actor.CanAcquire(item);
        return check.Ok
            ? ActionRuleResult.Pass()
            : ActionRuleResult.Fail(check.Reason!);
    }
}
