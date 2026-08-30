using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative.Rules.Choice;

/// <summary>
/// When a <see cref="MoralLevel.Low"/> thinking modus mentis is offered a crime among the honest
/// options, the crime is the only thing it is offered.
///
/// <para>The mirror of <see cref="HighMoralityAvoidsCrimeRule"/>, and the reason both are needed: a
/// principled mind and an amoral one should not be looking at the same list and merely rolling
/// differently on it. An amoral modus mentis shown a purse and a haystack is not weighing them.</para>
///
/// <para>Inert when nothing on offer is a crime — which is most of the time, and is why this reads as
/// a bias rather than as a straitjacket. The persona still chooses <i>which</i> crime, and can still
/// decline the lot through the hidden decline option the goal selector always carries.</para>
/// </summary>
public class LowMoralityPrefersCrimeRule : IGoalChoiceRule
{
    public IReadOnlyList<NarrativeAnchor> Filter(IReadOnlyList<NarrativeAnchor> offered, ChoiceRuleContext ctx)
    {
        if (ctx.ModusMentis.MoralLevel != MoralLevel.Low) return offered;

        var crimes = offered.Where(o => CrimeGoals.IsCrime(o, ctx)).ToList();
        if (crimes.Count == 0 || crimes.Count == offered.Count) return offered;

        Console.WriteLine($"ChoiceRules: [{ctx.ModusMentis.DisplayName}] is unscrupulous — offering only "
                          + $"the {crimes.Count} illegal goal(s) of {offered.Count}.");
        return crimes;
    }
}
