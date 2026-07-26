using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.ModiMentis;

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
    /// <summary>
    /// Modus-mentis types granted (resolved against <see cref="ModusMentisRegistry"/>).
    /// Declared as concrete types rather than string ids so that a mistyped or renamed
    /// modusMentis is a compile error at the catalog rather than a fragment that silently
    /// grants nothing at runtime.
    /// </summary>
    public IReadOnlyList<Type> SkillTypes { get; }

    /// <summary>Item factories invoked when REMEMBER fires.</summary>
    public IReadOnlyList<Func<Item>> Items { get; }

    /// <summary>
    /// Coins credited to the shared party wallet when REMEMBER fires. Coins live in the
    /// wallet, never the inventory, so a "gold coin you stole" grant bumps <see cref="Party"/>
    /// rather than materialising a carriable item.
    /// </summary>
    public IReadOnlyList<(CoinType Type, int Amount)> Coins { get; }

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
        IReadOnlyList<Type>? skillTypes = null,
        IReadOnlyList<Func<Item>>? items = null,
        IReadOnlyList<(CoinType Type, int Amount)>? coins = null,
        string? setChildhoodLocation = null,
        string nextReminescenceId = "<END>")
    {
        SkillTypes           = skillTypes ?? Array.Empty<Type>();
        Items                = items ?? Array.Empty<Func<Item>>();
        Coins                = coins ?? Array.Empty<(CoinType, int)>();
        SetChildhoodLocation = setChildhoodLocation;
        NextReminescenceId   = nextReminescenceId;

        // typeof(X) fixes the *spelling* at compile time but not the *kind*: Type itself is
        // unconstrained, so a non-modusMentis would still compile. Fail loudly while the catalog
        // is being built rather than skipping the grant when the player reaches the fragment.
        foreach (var type in SkillTypes)
        {
            if (!typeof(ModusMentis).IsAssignableFrom(type) || type.IsAbstract)
                throw new ArgumentException(
                    $"FragmentOutcome: '{type.Name}' is not a concrete ModusMentis.", nameof(skillTypes));
        }
    }

    /// <summary>True when this fragment terminates the childhood reminescence phase.</summary>
    public bool IsTerminal =>
        string.Equals(NextReminescenceId, "<END>", StringComparison.OrdinalIgnoreCase);
}
