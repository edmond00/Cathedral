using System;

namespace Cathedral.Game.Narrative;

/// <summary>
/// One clause of a modus mentis's emotional disposition: "when an action produces <b>this kind</b> of
/// consequence, I feel <b>this</b>."
///
/// <para><b>The match is on the outcome's TYPE, never on its payload.</b> That is the whole discipline
/// of the system and it is deliberate. Asking "was the item <i>fine</i>?" or "was the target <i>weaker
/// than me</i>?" would put an open-ended question in front of every consequence in the game, and the
/// answer would have to come from an LLM — one more request per action, for a decision the player
/// never sees. A type is a fact the compiler already knows. The cost is real and worth naming: an
/// <c>ItemAcquisitionOutcome</c> cannot tell gluttony that the item was bread, so gluttony is not an
/// emotion modus mentis. That is the correct outcome, not a limitation to route around.</para>
///
/// <para><see cref="WhenSeverity"/> is the one concession, and it reads no payload either — 
/// <see cref="Outcome.Severity"/> is on the base class. It exists for
/// <c>AffinityIncrementOutcome</c>, which is one type carrying two opposite pieces of news (its
/// severity is set from the delta's sign in its own constructor), so a type-only match would make
/// pride feel affronted by being liked.</para>
/// </summary>
/// <param name="OutcomeType">The <see cref="Outcome"/> subclass that fires this. Assignable matches
/// count, so a trigger on a base type catches its subclasses.</param>
/// <param name="Humor">Factory for the humor produced. A factory rather than an instance because a
/// queue holds one object per slot — 1d6 of them per trigger — and a shared instance would put the
/// same object in six places.</param>
/// <param name="WhenSeverity">When set, the outcome must also carry this severity. Null = any.</param>
public readonly record struct EmotionTrigger(
    Type OutcomeType,
    Func<BodyHumor> Humor,
    OutcomeSeverity? WhenSeverity = null)
{
    /// <summary>True when <paramref name="outcome"/> is the kind of news this trigger answers.</summary>
    public bool Matches(Outcome outcome)
        => OutcomeType.IsInstanceOfType(outcome)
           && (WhenSeverity == null || outcome.Severity == WhenSeverity.Value);
}
