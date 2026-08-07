using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Picks up a <b>man-made</b> item from a <see cref="PointOfInterest"/> in the current area or spot.
/// Natural/wild resources use <see cref="GatherVerb"/>; private areas use <see cref="StealVerb"/>;
/// corpse parts use <see cref="CutVerb"/>.
/// </summary>
public class GrabVerb : Verb
{
    public override string VerbId         => "grab";
    public override string DisplayName    => "Grab";
    public override int    BaseDifficulty => 1;

    /// <summary>Taking a made thing off a shelf and carrying it. No hands, no verb.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    /// <summary>What a success teaches: taking a made thing cleanly off its shelf.</summary>
    public override string? GrantedModusMentisId(Element? target) => "finesse";

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
    {
        if (target is not ItemElement itemEl) return false;
        if (pov.Where.IsPrivate) return false;                 // private → Steal

        var poi = ItemPickup.FindHoldingPoI(pov, itemEl);
        return poi != null && !poi.IsNatural;                  // natural → Gather
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => PickupVerbatim("grab", target);

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        if (target is not ItemElement itemElement) return System.Array.Empty<OutcomeReport>();
        return new[] { new ItemAcquisitionOutcome(itemElement) };
    }

    public override Item? AcquiredItem(Element? target) => (target as ItemElement)?.Item;
}
