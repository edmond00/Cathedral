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
        // First: the only rule about the body rather than the situation. A player told a witness is
        // watching will walk to another room; one told their arm is ruined will not.
        new BrokenModusMentisRule(),
        new ZeroRepliesDialogueRule(),
        new NegativeAffinityDialogueRule(),
        new IllegalActionHighMoralityRule(),
        new IllegalActionVisualWitnessRule(),
        new UnderThreatImpossibleRule(),
        new ConfrontedImpossibleRule(),
        new RequiredToolRule(),
        new FirstBlowMediumRule(),
        new InventoryCapacityRule(),
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

    /// <summary>
    /// The same rules asked ahead of time, while a thinking modus mentis is still choosing — before any
    /// button exists, and so before any implement can have been combined with it. Used by iterative
    /// thinking (<c>ThinkingIterationsStat</c>) to try something else rather than offer an act the rules
    /// would refuse on the click.
    ///
    /// <para>The rules that read the combined item are left out, since none has been combined yet and
    /// combining one is how the player answers them: <see cref="RequiredToolRule"/> (the act stays
    /// standing for "Use Tool") and <see cref="FirstBlowMediumRule"/> (a weapon in hand changes the
    /// medium). Nothing here has side effects; the click still runs <see cref="Check"/> in full.</para>
    /// </summary>
    public static ActionRuleResult Probe(ActionRuleContext ctx)
    {
        foreach (var rule in Rules)
        {
            if (rule is RequiredToolRule or FirstBlowMediumRule) continue;
            var result = rule.Check(ctx);
            if (!result.Passed)
                return result;
        }
        return ActionRuleResult.Pass();
    }
}
