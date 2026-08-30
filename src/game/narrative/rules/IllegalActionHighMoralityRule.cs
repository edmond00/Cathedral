using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Narrative.Rules;

/// <summary>
/// Blocks illegal actions when the chosen action modus mentis has <see cref="MoralLevel.High"/>.
/// A principled modus mentis refuses to participate in theft, trespass, or violence — even
/// if the player selects it. The player must switch to a less scrupulous modus mentis.
///
/// <para>The action-side half of the morality design; the thinking-side half is
/// <c>HighMoralityAvoidsCrimeRule</c>, which keeps crimes out of a principled mind's goal list in the
/// first place. Both are needed: the goal and the means are two separate choices, so a scrupulous
/// skill can still be picked to carry out a goal some other modus mentis chose.</para>
/// </summary>
public class IllegalActionHighMoralityRule : IActionRule
{
    public ActionRuleResult Check(ActionRuleContext ctx)
    {
        if (!ctx.IsIllegalAction) return ActionRuleResult.Pass();

        var mm = ctx.ActionModusMentis;
        if (mm == null || mm.MoralLevel != MoralLevel.High) return ActionRuleResult.Pass();

        return ActionRuleResult.Fail(
            $"Your {mm.DisplayName} recoils at the idea — this goes against every principle it holds.");
    }
}
