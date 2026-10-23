using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Narrative.Rules.Choice;

/// <summary>
/// Whether a modus mentis may take part in turning a sense on something: only one holding the
/// <see cref="ModusMentisFunction.Observation"/> function. Examining, contemplating, listening and
/// smelling are the observing office turned on one object, and a mind whose business is reckoning,
/// scheming or doing was offering them on nearly every object in the game — which is what crowded out
/// every other goal.
///
/// <para>The two halves of the chain are held to it differently. The thinking modus mentis of a
/// <b>human</b> is never offered any of the four senses as a goal
/// (<see cref="SensoryGoalNeedsObservationRule"/>); a beast's is offered them all, because for a beast
/// the senses are most of what there is to do, and a wolf that could only sniff when a nose-minded
/// faculty happened to be thinking would stand idle. An action modus mentis asked to carry out
/// <b>examining</b>, whatever the body, refuses outright (<see cref="ExamineActionNeedsObservationRule"/>);
/// the other three senses are carried out by whoever is asked.</para>
/// </summary>
internal static class ObservationGoals
{
    public static bool IsSensory(NarrativeAnchor? goal) => goal is VerbAction { Verb: SensoryVerb };

    public static bool IsExamine(NarrativeAnchor? goal) => goal is VerbAction { Verb: ExamineVerb };

    // A will carries the observing office too (narrowed to its path), so it counts as an observer here.
    public static bool Observes(ModusMentis mm) => mm.CanObserve;
}

/// <summary>A human's thinking modus mentis without the Observation function is never offered a sense.</summary>
public class SensoryGoalNeedsObservationRule : IGoalChoiceRule
{
    public IReadOnlyList<NarrativeAnchor> Filter(IReadOnlyList<NarrativeAnchor> offered, ChoiceRuleContext ctx)
    {
        if (ctx.Actor.AnatomyType != AnatomyType.Human) return offered;
        if (ObservationGoals.Observes(ctx.ModusMentis)) return offered;

        var kept = offered.Where(o => !ObservationGoals.IsSensory(o)).ToList();
        if (kept.Count == offered.Count) return offered;

        Console.WriteLine($"ChoiceRules: [{ctx.ModusMentis.DisplayName}] does not observe — withholding "
                          + "the goals of the senses.");
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
        if (!ObservationGoals.IsExamine(ctx.Goal)) return offered;
        if (ObservationGoals.Observes(ctx.ModusMentis)) return offered;

        if (!ctx.Probe)
            Console.WriteLine($"ChoiceRules: [{ctx.ModusMentis.DisplayName}] does not observe — refusing to examine.");
        return offered.Refusing(Reason);
    }
}
