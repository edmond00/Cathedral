using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Narrative.Rules.Choice;

/// <summary>
/// Whether a modus mentis may take part in examining at all: only one holding the
/// <see cref="ModusMentisFunction.Observation"/> function. Examining something closely is the
/// observing office turned on one object, and a mind whose business is reckoning, scheming or doing
/// was offering it on nearly every object in the game — which is what crowded out every other goal.
///
/// <para>Both halves of the chain are held to it, as the morality rules are: the thinking modus
/// mentis is never offered examining as a goal (<see cref="ExamineGoalNeedsObservationRule"/>), and
/// an action modus mentis asked to carry it out refuses outright
/// (<see cref="ExamineActionNeedsObservationRule"/>). Contemplating, listening and smelling are left
/// alone: they are offered far more rarely.</para>
/// </summary>
internal static class ExamineGoals
{
    public static bool IsExamine(NarrativeAnchor? goal) => goal is VerbAction { Verb: ExamineVerb };

    public static bool Observes(ModusMentis mm) => mm.Functions.Contains(ModusMentisFunction.Observation);
}

/// <summary>A thinking modus mentis without the Observation function is never offered examining.</summary>
public class ExamineGoalNeedsObservationRule : IGoalChoiceRule
{
    public IReadOnlyList<NarrativeAnchor> Filter(IReadOnlyList<NarrativeAnchor> offered, ChoiceRuleContext ctx)
    {
        if (ExamineGoals.Observes(ctx.ModusMentis)) return offered;

        var kept = offered.Where(o => !ExamineGoals.IsExamine(o)).ToList();
        if (kept.Count == offered.Count) return offered;

        Console.WriteLine($"ChoiceRules: [{ctx.ModusMentis.DisplayName}] does not observe — withholding "
                          + "the goal of examining.");
        return kept;
    }
}

/// <summary>
/// An action modus mentis without the Observation function refuses to examine, with no question put
/// to it: the refusal is the rule's, so the persona is not asked for one it might not give.
/// </summary>
public class ExamineActionNeedsObservationRule : IWillingnessRule
{
    /// <summary>First person, joined to "I don't want to examine X closely: …" by NeutralNarration.</summary>
    public const string Reason = "I want to act, not to stand and observe";

    public WillingnessOptions Filter(WillingnessOptions offered, ChoiceRuleContext ctx)
    {
        if (!ExamineGoals.IsExamine(ctx.Goal)) return offered;
        if (ExamineGoals.Observes(ctx.ModusMentis)) return offered;

        Console.WriteLine($"ChoiceRules: [{ctx.ModusMentis.DisplayName}] does not observe — refusing to examine.");
        return offered.Refusing(Reason);
    }
}
