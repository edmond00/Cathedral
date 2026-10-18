using System;

namespace Cathedral.Game.Narrative.Rules.Choice;

/// <summary>
/// A <see cref="MoralLevel.High"/> action modus mentis asked to carry out a crime refuses, with no
/// question put to it — the refusal is the rule's, as <see cref="ExamineActionNeedsObservationRule"/>'s
/// is, so the judge is not asked for one it might not give.
///
/// <para>The action-side half of the morality design, mirroring <see cref="HighMoralityAvoidsCrimeRule"/>
/// on the thinking side and <see cref="LowMoralityNeverRefusesCrimeRule"/> on this one. It used to be
/// an action rule (<see cref="IllegalActionHighMoralityRule"/>), checked only when the player clicked
/// the button — so a principled means was handed a crime some other mind wanted, offered as a button,
/// and refused at the cost of a second noetic point. Here the refusal happens during the thinking
/// that would have produced the button, and it reaches the proposal ledger: the goal is withheld from
/// the mind that chose it, and a second attempt is offered only means this rule lets through
/// (<c>ThinkingExecutor.CanCarryOut</c>).</para>
///
/// <para>Morality is character, not circumstance, which is why it belongs with the choice rules. The
/// action rule stays as a safety net for the paths that never ask willingness (auto-success phases).</para>
/// </summary>
public class HighMoralityRefusesCrimeRule : IWillingnessRule
{
    /// <summary>First person, joined to "I don't want to pick his pocket: …" by NeutralNarration.</summary>
    public const string Reason = "it goes against every principle I hold";

    public WillingnessOptions Filter(WillingnessOptions offered, ChoiceRuleContext ctx)
    {
        if (ctx.ModusMentis.MoralLevel != MoralLevel.High) return offered;
        if (!CrimeGoals.IsCrime(ctx.Goal, ctx)) return offered;
        if (!ctx.Probe)
            Console.WriteLine($"ChoiceRules: [{ctx.ModusMentis.DisplayName}] is principled — refusing an illegal action.");
        return offered.Refusing(Reason);
    }
}
