using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Reminescence;

/// <summary>
/// Static description of what a single childhood-reminescence fragment grants when
/// it is REMEMBERed. Outcomes are applied by <c>RememberVerb.Execute</c>:
///   - the named modusMentis is added to the protagonist via the standard acquisition procedure,
///   - the items are added via the equip / contain / hold / skip procedure,
///   - the childhood-history fields are mutated (location is set on the very first REMEMBER),
///   - the protagonist transitions to <see cref="NextReminescenceId"/>, or the phase ends if &lt;END&gt;.
/// </summary>
public sealed class FragmentOutcome
{
    /// <summary>Modus-mentis ids granted (resolved against <see cref="ModusMentisRegistry"/>).</summary>
    public IReadOnlyList<string> SkillIds { get; }

    /// <summary>Item factories invoked when REMEMBER fires.</summary>
    public IReadOnlyList<Func<Item>> Items { get; }

    /// <summary>
    /// When non-null, sets <see cref="ChildhoodHistory.Location"/> on the protagonist.
    /// Set only by the first reminescence (<c>sound_in_the_dark</c>).
    /// </summary>
    public string? SetChildhoodLocation { get; }

    /// <summary>
    /// Reminescence id to transition to when this fragment is remembered, or "&lt;END&gt;"
    /// to end the childhood reminescence phase.
    /// </summary>
    public string NextReminescenceId { get; }

    public FragmentOutcome(
        IReadOnlyList<string>? skillIds = null,
        IReadOnlyList<Func<Item>>? items = null,
        string? setChildhoodLocation = null,
        string nextReminescenceId = "<END>")
    {
        SkillIds             = skillIds ?? Array.Empty<string>();
        Items                = items ?? Array.Empty<Func<Item>>();
        SetChildhoodLocation = setChildhoodLocation;
        NextReminescenceId   = nextReminescenceId;
    }

    /// <summary>True when this fragment terminates the childhood reminescence phase.</summary>
    public bool IsTerminal =>
        string.Equals(NextReminescenceId, "<END>", StringComparison.OrdinalIgnoreCase);
}
