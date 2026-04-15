namespace Cathedral.Game.Narrative.Rules;

/// <summary>
/// A single hardcoded plausibility rule checked before the LLM critic.
/// Rules are deterministic and fast — no LLM calls.
/// Add new rules by implementing this interface and registering in <see cref="ActionRulesChecker"/>.
/// </summary>
public interface IActionRule
{
    ActionRuleResult Check(ActionRuleContext ctx);
}
