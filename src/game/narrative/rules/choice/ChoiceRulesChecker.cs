using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Rules.Choice;

/// <summary>
/// Runs the coded rules that narrow what a modus mentis is offered, in declaration order. The
/// counterpart to <see cref="ActionRulesChecker"/>, and deliberately shaped like it: an ordered list
/// of tiny classes, one entry point per stage, nothing clever in between.
///
/// <para>Where the two differ is what a failure means. An action rule <i>rejects</i> — the player
/// chose something and is told no, at the cost of a noetic point. A choice rule <i>withholds</i> —
/// the option is absent, silently, and there is nothing to explain because nothing was refused. That
/// makes the choice rules the right home for anything about character (a principled mind does not
/// think about burglary) and the action rules the right home for anything about circumstance (that
/// door is locked, that man is watching).</para>
///
/// <para>To add a rule: implement <see cref="IGoalChoiceRule"/> or <see cref="IWillingnessRule"/> and
/// add one line to the matching list below. Rules compose — each sees what the previous one left —
/// so two that narrow the same list are free to disagree, and the later one wins.</para>
/// </summary>
public static class ChoiceRulesChecker
{
    /// <summary>Rules that narrow the goals a thinking modus mentis may be offered.</summary>
    private static readonly IReadOnlyList<IGoalChoiceRule> GoalRules = new List<IGoalChoiceRule>
    {
        new HighMoralityAvoidsCrimeRule(),
        new LowMoralityPrefersCrimeRule(),
    };

    /// <summary>Rules that narrow how an action modus mentis may answer "do you want to do it?".</summary>
    private static readonly IReadOnlyList<IWillingnessRule> WillingnessRules = new List<IWillingnessRule>
    {
        new LowMoralityNeverRefusesCrimeRule(),
    };

    /// <summary>Narrows <paramref name="goals"/> to what this thinking modus mentis may be shown.</summary>
    public static IReadOnlyList<ConcreteOutcome> FilterGoals(
        IReadOnlyList<ConcreteOutcome> goals, ChoiceRuleContext ctx)
    {
        foreach (var rule in GoalRules)
            goals = rule.Filter(goals, ctx);
        return goals;
    }

    /// <summary>Narrows <paramref name="options"/> to the answers this action modus mentis may give.</summary>
    public static WillingnessOptions FilterWillingness(WillingnessOptions options, ChoiceRuleContext ctx)
    {
        foreach (var rule in WillingnessRules)
            options = rule.Filter(options, ctx);
        return options;
    }
}
