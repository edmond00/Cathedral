using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Rules;

/// <summary>
/// Runs all registered coded plausibility rules in order and returns the first failure.
/// Rules are checked before the LLM critic — they are fast, deterministic, and absolute
/// (a failing rule cannot be overridden by noetic points).
///
/// To add a new rule: implement <see cref="IActionRule"/> and add an instance to <see cref="Rules"/>.
/// </summary>
public static class ActionRulesChecker
{
    /// <summary>
    /// Ordered list of rules evaluated on every action.
    /// Rules are checked in declaration order; the first failure short-circuits the rest.
    /// </summary>
    private static readonly IReadOnlyList<IActionRule> Rules = new List<IActionRule>
    {
        new NegativeAffinityDialogueRule(),
        new IllegalActionHighMoralityRule(),
        new IllegalActionVisualWitnessRule(),
    };

    /// <summary>
    /// Evaluates all rules against <paramref name="ctx"/>.
    /// Returns <see cref="ActionRuleResult.Pass()"/> if every rule passes,
    /// or the first <see cref="ActionRuleResult.Fail"/> encountered.
    /// </summary>
    public static ActionRuleResult Check(ActionRuleContext ctx)
    {
        foreach (var rule in Rules)
        {
            var result = rule.Check(ctx);
            if (!result.Passed)
                return result;
        }
        return ActionRuleResult.Pass();
    }
}
