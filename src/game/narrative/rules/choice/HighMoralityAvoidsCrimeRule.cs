using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative.Rules.Choice;

/// <summary>
/// A <see cref="MoralLevel.High"/> thinking modus mentis is never offered a crime as a goal.
///
/// <para>The thinking-side half of the morality design. <see cref="IllegalActionHighMoralityRule"/>
/// already stops a principled modus mentis from <i>carrying out</i> a crime, but only at the moment of
/// execution — which reads as a mind that conceived a burglary, chose how to do it, and then balked on
/// the doorstep. Filtering the goal list instead means the thought never forms: a principled modus
/// mentis looking at a locked chest simply sees a locked chest.</para>
///
/// <para>Removing every goal is a legitimate result. The caller treats an empty list as ignore, which
/// is the right reading — there was nothing here this mind was willing to want.</para>
/// </summary>
public class HighMoralityAvoidsCrimeRule : IGoalChoiceRule
{
    public IReadOnlyList<ConcreteOutcome> Filter(IReadOnlyList<ConcreteOutcome> offered, ChoiceRuleContext ctx)
    {
        if (ctx.ModusMentis.MoralLevel != MoralLevel.High) return offered;

        var lawful = offered.Where(o => !CrimeGoals.IsCrime(o, ctx)).ToList();
        if (lawful.Count == offered.Count) return offered;

        Console.WriteLine($"ChoiceRules: [{ctx.ModusMentis.DisplayName}] is principled — withholding "
                          + $"{offered.Count - lawful.Count} illegal goal(s) of {offered.Count}.");
        return lawful;
    }
}
