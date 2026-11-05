using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc.Corpse;

using Cathedral.Game.Narrative.ModiMentis;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Steals an item from where it is not free to take (<see cref="PrivacyModel.IsFreeToTake"/>): a
/// private room, always; a protected floor, until its master gives leave.
/// Functionally identical to <see cref="GrabVerb"/> but marks the action as illegal,
/// which triggers witness detection and the "caught red-handed" dialogue on failure.
/// </summary>
public class StealVerb : Verb
{

    public override string VerbId         => "steal";
    public override string DisplayName    => "Steal";
    public override int    BaseDifficulty => 3;

    /// <summary>Lays hands on what belongs to the place.</summary>
    public override VerbConduct Conduct => VerbConduct.Intrusive;

    /// <summary>Lifting something and carrying it away. No hands, no verb.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    /// <summary>What a success teaches: taking what is not yours, quietly.</summary>
    public override string? GrantedModusMentisId(Element? target) => "petty_thief";

    /// <summary>
    /// Taking what is not yours is a crime wherever it happens. Unconditional here rather than left
    /// to the setting tests: a leave given or a stay tolerated lifts the trespass in a private room,
    /// and what lies there is still not the taker's.
    /// </summary>
    protected override bool IsIllegalFor(Scene scene, PoV pov, Element? target, PartyMember? actor) => true;

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
    {
        if (target is not ItemElement itemEl) return false;
        if (PrivacyModel.IsFreeToTake(scene, pov)) return false;

        // A corpse's parts are cut, never stolen — you do not pickpocket a carcass for its own hide.
        // What the dead were carrying is a plain PoI beside the body, so it still steals normally.
        return pov.Where.PointsOfInterest
            .Where(poi => poi is not CorpsePointOfInterest)
            .Any(poi => poi.Items.Any(ie => ie.Id == itemEl.Id));
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => PickupVerbatim("steal", target);

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        if (target is not ItemElement itemElement) return System.Array.Empty<Outcome>();
        return new[] { new ItemAcquisitionOutcome(itemElement) };
    }

    public override Item? AcquiredItem(Element? target) => (target as ItemElement)?.Item;
}
