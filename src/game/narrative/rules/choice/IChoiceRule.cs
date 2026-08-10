using System.Collections.Generic;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.Rules.Choice;

/// <summary>
/// What a choice rule knows. One context serves both rule families because both are asked the same
/// underlying question — is what this modus mentis is being offered something it would touch? — at
/// two different moments of the same phase.
///
/// <para><see cref="ModusMentis"/> is whichever one is choosing at that moment: the thinking modus
/// mentis when goals are being filtered, the action modus mentis when its willingness is. A rule
/// therefore never has to ask which stage it is in.</para>
/// </summary>
/// <param name="Scene">The live scene, for the contextual legality test.</param>
/// <param name="PoV">Where and when the chooser is standing.</param>
/// <param name="Actor">The party member acting — not necessarily the protagonist.</param>
/// <param name="ModusMentis">The modus mentis whose choice is being narrowed.</param>
/// <param name="Goal">
/// The goal already settled on, when there is one. Null while the goal itself is what is being
/// chosen; set by the time willingness is asked, so a willingness rule can see what it is being
/// asked to assent to.
/// </param>
public sealed record ChoiceRuleContext(
    Scene.Scene      Scene,
    PoV              PoV,
    PartyMember      Actor,
    ModusMentis      ModusMentis,
    NarrativeAnchor? Goal = null);

/// <summary>
/// A deterministic, coded restriction on what a modus mentis may be <b>offered</b> — as opposed to
/// <see cref="IActionRule"/>, which judges an action already chosen.
///
/// <para>The distinction is worth keeping: an <see cref="IActionRule"/> failure is a refusal the
/// player sees and pays a noetic point for, while a choice rule is silent — the option was never on
/// the list, so nothing has to be explained and nothing is spent. Anything that should read as "I
/// will not do that" belongs in the action rules; anything that should read as "that never occurred
/// to me" belongs here.</para>
///
/// <para>Both families are lists of small classes registered in one place, so adding a rule is
/// writing one file and adding one line. See <see cref="ChoiceRulesChecker"/>.</para>
/// </summary>
/// <typeparam name="T">What the rule narrows: a list of goals, a set of willingness options.</typeparam>
public interface IChoiceRule<T>
{
    /// <summary>
    /// Returns <paramref name="offered"/> narrowed, or unchanged when the rule has nothing to say.
    /// Rules are applied in registration order, each seeing what the previous one left.
    /// </summary>
    T Filter(T offered, ChoiceRuleContext ctx);
}

/// <summary>A rule that narrows the goals a thinking modus mentis is offered.</summary>
public interface IGoalChoiceRule : IChoiceRule<IReadOnlyList<NarrativeAnchor>> { }

/// <summary>A rule that narrows how an action modus mentis may answer "do you want to do it?".</summary>
public interface IWillingnessRule : IChoiceRule<WillingnessOptions> { }
