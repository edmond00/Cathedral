namespace Cathedral.Game.Narrative.Rules.Choice;

/// <summary>
/// A <see cref="MoralLevel.Low"/> action modus mentis cannot refuse a crime on principle — it has
/// none to refuse on. The decline option is withheld, leaving it to answer with a degree of assent:
/// eager, willing, or reluctant.
///
/// <para>Reluctance survives deliberately. An amoral modus mentis may still find a particular job
/// beneath it, badly timed or not worth the trouble, and that shows up where it should — as +1
/// difficulty — rather than as a veto. What it may not do is decline out of scruple.</para>
///
/// <para>Only crimes are affected. Asked to do something lawful and disagreeable, a Low modus mentis
/// declines like anybody else.</para>
/// </summary>
public class LowMoralityNeverRefusesCrimeRule : IWillingnessRule
{
    public WillingnessOptions Filter(WillingnessOptions offered, ChoiceRuleContext ctx)
    {
        if (ctx.ModusMentis.MoralLevel != MoralLevel.Low) return offered;
        if (offered.DeclineOption == null) return offered;
        if (!CrimeGoals.IsCrime(ctx.Goal, ctx)) return offered;

        Console.WriteLine($"ChoiceRules: [{ctx.ModusMentis.DisplayName}] is unscrupulous — "
                          + "no refusal offered for an illegal action.");
        return offered.WithoutDecline();
    }
}
