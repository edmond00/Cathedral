namespace Cathedral.Game.Narrative.Rules;

/// <summary>
/// A witness confronting the actor (see <see cref="Scene.ConfrontationSelector"/>) blocks every act
/// but the few that answer them — the witness-side twin of <see cref="UnderThreatImpossibleRule"/>.
/// Somebody who heard a crime and walked over does not stand by while the actor gathers radishes or
/// strolls off down the track; the actor faces them, strikes at them, runs (the RUNAWAY exit), or
/// acts discreetly, which slips past this as it slips past the threat rule.
///
/// <para>Before this a heard crime cost one move action: walk to the next area, and the witness, who
/// stays where they were drawn to, was no longer in the room.</para>
/// </summary>
public class ConfrontedImpossibleRule : IActionRule
{
    public ActionRuleResult Check(ActionRuleContext ctx)
    {
        if (ctx.Confronter == null) return ActionRuleResult.Pass();
        if (ctx.Action.Verb.CanBeUsedWhenConfronted) return ActionRuleResult.Pass();
        if (ctx.ActionModusMentis?.ActsDiscretely == true) return ActionRuleResult.Pass();

        var name = Cathedral.Game.Npc.NpcLabelResolver.ShortLabelCapitalized(ctx.Confronter, ctx.Actor);
        return ActionRuleResult.Fail($"{name} is right here, watching me — I cannot simply carry on as if nothing happened.");
    }
}
