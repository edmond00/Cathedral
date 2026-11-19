using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Narrative.Rules.Choice;

/// <summary>
/// A member who is <b>confronted</b> — an enemy in the room, or a witness with a reason to stop them —
/// is offered only the acts that answer the confrontation: what an enemy permits
/// (<see cref="Verb.CanBeUsedUnderThreat"/>: strike, kill, appease, reconcile, tame) or what a
/// confronting witness permits (<see cref="Verb.CanBeUsedWhenConfronted"/>: strike, kill, explain
/// oneself).
///
/// <para>The rules of action already refuse everything else when it is attempted
/// (<see cref="UnderThreatImpossibleRule"/>, <see cref="ConfrontedImpossibleRule"/>), but only then:
/// the thought that conceived it had cost a point, and the act it left waiting would cost another to be
/// refused. A mind facing a man who has just caught it does not think of introducing itself; the
/// thought simply never forms, and nothing is spent on it.</para>
///
/// <para>One exception, the one the rules of action make: a <b>discreet</b> action modus mentis may
/// attempt any act at close quarters, so while the member holds one
/// (<see cref="ChoiceRuleContext.Means"/>), a goal only discretion allows is still offered. Read the
/// same way <see cref="DiscreetNeverRefusesCoverRule"/> reads it, an enemy outranking a witness.</para>
/// </summary>
public class ConfrontedOffersOnlyAnswersRule : IGoalChoiceRule
{
    public IReadOnlyList<NarrativeAnchor> Filter(IReadOnlyList<NarrativeAnchor> offered, ChoiceRuleContext ctx)
    {
        if (ConfrontationSelector.Current(ctx.Scene, ctx.PoV, ctx.Actor) is not { } confrontation) return offered;
        if (ctx.Means?.Any(m => m.ActsDiscretely) == true) return offered;

        var answers = offered.Where(o => o is not VerbAction act || Answers(act.Verb, confrontation)).ToList();
        if (answers.Count == offered.Count) return offered;

        if (!ctx.Probe)
            Console.WriteLine($"ChoiceRules: [{ctx.ModusMentis.DisplayName}] is confronted by "
                              + $"{confrontation.Npc.DisplayName} — withholding {offered.Count - answers.Count} "
                              + $"goal(s) of {offered.Count} that do not answer them.");
        return answers;
    }

    /// <summary>Whether <paramref name="verb"/> is one the confrontation permits without discretion.</summary>
    public static bool Answers(Verb verb, Confrontation confrontation)
        => confrontation.IsWitness ? verb.CanBeUsedWhenConfronted : verb.CanBeUsedUnderThreat;
}
