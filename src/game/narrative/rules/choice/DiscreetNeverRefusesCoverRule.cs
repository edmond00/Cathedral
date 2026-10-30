using System;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Narrative.Rules.Choice;

/// <summary>
/// A <b>discreet</b> action modus mentis cannot refuse an act that only discretion makes possible
/// here — the discreet twin of <see cref="LowMoralityNeverRefusesCrimeRule"/>. Acting under somebody's
/// eyes is what discreteness is for, and a discreet mind declining exactly the act no other mind could
/// attempt made the trait worth nothing at the moment it was needed.
///
/// <para>"Only discretion makes possible" is read off the three rules of action that stop a
/// non-discreet means and let a discreet one through, asked as the click would ask them of a means
/// without the trait (<see cref="NeedsDiscretion"/>): a crime in sight of a witness
/// (<see cref="IllegalActionVisualWitnessRule"/>), an enemy in the room
/// (<see cref="UnderThreatImpossibleRule"/>), a witness confronting the actor
/// (<see cref="ConfrontedImpossibleRule"/>). Out of sight of everybody, a discreet mind declines like
/// any other.</para>
///
/// <para>Reluctance survives, as it does for the unscrupulous: a discreet mind may still think the
/// moment badly chosen, and that shows as +1 difficulty rather than a veto. A refusal another rule
/// imposes — a principled mind asked for a crime — is not lifted: <see cref="WillingnessOptions.WithoutDecline"/>
/// drops the option, not the verdict.</para>
/// </summary>
public class DiscreetNeverRefusesCoverRule : IWillingnessRule
{
    public WillingnessOptions Filter(WillingnessOptions offered, ChoiceRuleContext ctx)
    {
        if (!ctx.ModusMentis.ActsDiscretely) return offered;
        if (offered.DeclineOption == null) return offered;
        if (ctx.Goal is not VerbAction act || !NeedsDiscretion(act, ctx)) return offered;

        if (!ctx.Probe)
            Console.WriteLine($"ChoiceRules: [{ctx.ModusMentis.DisplayName}] is discreet — "
                              + "no refusal offered for an act only discretion allows here.");
        return offered.WithoutDecline();
    }

    /// <summary>
    /// Whether a means without discreteness would be stopped from attempting <paramref name="act"/>
    /// here by one of the three rules of action that discreteness passes. Read with the protagonist's
    /// affinity key, which is the one the click's context is built with whoever is acting.
    /// </summary>
    public static bool NeedsDiscretion(VerbAction act, ChoiceRuleContext ctx)
    {
        const string key = Protagonist.AffinityKeyConstant;
        var verb = act.Verb;

        // An enemy in sight: UnderThreatImpossibleRule. It also outranks every witness as the
        // confrontation, so no confronter is read beside it (ConfrontationSelector.Current).
        var threat = ThreatSelector.ComputeContext(ctx.Scene, ctx.PoV, key);
        bool enemyInSight = threat.Level == ThreatLevel.Visual;
        if (enemyInSight && !verb.CanBeUsedUnderThreat) return true;

        // A witness confronting the actor: ConfrontedImpossibleRule.
        var confronter = enemyInSight && threat.Threat != null ? null : ConfrontationSelector.Confronter(ctx.Scene, ctx.PoV, key);
        if (confronter != null && !verb.CanBeUsedWhenConfronted) return true;

        // A crime in sight of a witness: IllegalActionVisualWitnessRule, with its exemption for an act
        // that answers the one already confronting.
        return CrimeGoals.IsCrime(act, ctx)
               && WitnessSelector.ComputeContext(ctx.Scene, ctx.PoV).Type == WitnessType.Visual
               && !(confronter != null && verb.CanBeUsedWhenConfronted);
    }
}
