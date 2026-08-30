using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.Rules.Choice;

/// <summary>
/// The one place a choice rule asks whether a goal is a crime, so three rules cannot come to three
/// different answers about the same purse.
/// </summary>
internal static class CrimeGoals
{
    /// <summary>
    /// Whether pursuing <paramref name="goal"/> from where the chooser stands would be a crime.
    /// Non-verb goals (and a null one) are never crimes: there is no verb to be illegal.
    /// </summary>
    public static bool IsCrime(NarrativeAnchor? goal, ChoiceRuleContext ctx)
        => goal is VerbAction vo
           && vo.Verb.IsIllegal(ctx.Scene, ctx.PoV, vo.Target, ctx.Actor);
}
