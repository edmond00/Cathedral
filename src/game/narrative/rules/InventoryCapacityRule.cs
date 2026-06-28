namespace Cathedral.Game.Narrative.Rules;

/// <summary>
/// Blocks a pickup action (grab/gather/steal/cut) when the acting member has nowhere to put the item.
/// Deterministic and absolute — checked before the LLM critic. Pickup verbs report the item they would
/// acquire via <c>Verb.AcquiredItem</c>; non-pickup actions pass unconditionally.
/// </summary>
public class InventoryCapacityRule : IActionRule
{
    public ActionRuleResult Check(ActionRuleContext ctx)
    {
        var outcome = ctx.Action.PreselectedOutcome;
        var item = outcome?.VerbView.Verb.AcquiredItem(outcome.Target);
        if (item == null) return ActionRuleResult.Pass();          // not a pickup

        if (ctx.Actor.CanAcquireItem(item)) return ActionRuleResult.Pass();

        return ActionRuleResult.Fail(
            $"There is nowhere to put the {item.DisplayName.ToLowerInvariant()} — you cannot carry any more.");
    }
}
