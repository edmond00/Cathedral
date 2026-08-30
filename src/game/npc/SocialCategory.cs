namespace Cathedral.Game.Npc;

/// <summary>
/// The social standing an NPC identifies with. Worn garments that read well to a standing grant
/// bonus dialogue dice when speaking to someone of that standing — dressing like a townsman opens
/// a craftsman's door, and opens nothing at all with a hermit.
///
/// Every speaking archetype declares one (<c>NamedNpcArchetype.Social</c>); <c>--npc-audit</c>
/// fails if any does not. Several members are deliberately unused by the current roster:
/// <see cref="Aristocrat"/>, <see cref="Military"/> and <see cref="Urban"/> exist because garments
/// appealing to them are already being authored and the archetypes are the obvious next families.
/// The audit reports empty standings as informational, not as warnings.
/// </summary>
public enum SocialCategory
{
    /// <summary>Craftsmen, millers, shopkeepers — those who own their trade.</summary>
    Bourgeois,

    /// <summary>Gentry and titled households. No archetype yet.</summary>
    Aristocrat,

    /// <summary>Those who work another's land: farmhands, herders, bondmen.</summary>
    Peasant,

    /// <summary>Men-at-arms, guards, anyone who carries a weapon for a living. No archetype yet.</summary>
    Military,

    /// <summary>Townsfolk without a trade of their own. No archetype yet.</summary>
    Urban,

    /// <summary>Those set apart by devotion — druids, hermits.</summary>
    Religious,

    /// <summary>Those outside the law and content there.</summary>
    Outlaw,

    /// <summary>
    /// The destitute — beggars, vagrants, anyone with nothing. The standing that plain, worn or
    /// makeshift dress speaks to: rough clothes are no recommendation anywhere else, but among
    /// people who have nothing they mark you as one of them rather than as someone to be wary of.
    ///
    /// This exists so that <em>every</em> garment says something to somebody. Without it the
    /// humblest items — a sack-cloth mitt, a belt, an empty purse — appealed to no standing at
    /// all and were pure dead weight on an anchor.
    /// </summary>
    Pauper,
}
