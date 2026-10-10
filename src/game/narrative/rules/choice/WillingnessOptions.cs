using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative.Rules.Choice;

/// <summary>
/// The answers an action modus mentis may give to "do you want to do it?".
///
/// <para><see cref="Stances"/> are the degrees of assent, strongest first — each maps to a difficulty
/// modifier. <see cref="DeclineOption"/> is the refusal, carried separately because it is not a degree
/// of anything: it rides into the selector as the decline option and cancels the action outright. A
/// rule that removes it is saying this modus mentis has no refusal available to it, not that refusing
/// has become harder.</para>
///
/// <para>Immutable, so a rule returns a narrowed copy rather than editing the set the next rule is
/// about to read.</para>
/// </summary>
public sealed record WillingnessOptions(IReadOnlyList<string> Stances, string? DeclineOption)
{
    /// <summary>
    /// Set when a rule has already decided the answer is no, with the first-person reason why. The
    /// modus mentis is then not asked at all — a refusal the persona might decline to give is no rule —
    /// and the reason is written into the refusal itself rather than left for the rewrite to supply.
    /// </summary>
    public string? ImposedRefusal { get; init; }

    /// <summary>Imposes the refusal, for <paramref name="reason"/>.</summary>
    public WillingnessOptions Refusing(string reason) => this with { ImposedRefusal = reason };

    /// <summary>Drops the refusal, leaving the modus mentis to answer only with a degree of assent.</summary>
    public WillingnessOptions WithoutDecline() => this with { DeclineOption = null };

    /// <summary>Drops any stance matching <paramref name="stance"/>, keeping the order of the rest.</summary>
    public WillingnessOptions Without(string stance)
        => this with { Stances = Stances.Where(s => s != stance).ToList() };
}
