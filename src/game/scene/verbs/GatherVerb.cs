using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Gathers a <b>natural/wild</b> resource item (from a <see cref="PointOfInterest.IsNatural"/> PoI)
/// in the current area or spot. The natural counterpart of <see cref="GrabVerb"/>, and the only
/// pickup verb recordable as a routine (so "gather mushrooms here" can be replayed). Depletion +
/// regeneration apply to all picked items; natural resources regrow faster (see <c>ResourceRegen</c>).
/// </summary>
public class GatherVerb : Verb
{
    public override string VerbId         => "gather";
    public override string DisplayName    => "Gather";
    public override int    BaseDifficulty => 1;

    /// <summary>Picking and carrying off what grows. No hands, no verb.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    /// <summary>What a success teaches: taking what grows is foraging.</summary>
    public override string? GrantedModusMentisId(Element? target) => "forage_lore";

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
    {
        if (target is not ItemElement itemEl) return false;
        if (pov.Where.IsPrivate) return false;                 // private → Steal

        var poi = ItemPickup.FindHoldingPoI(pov, itemEl);
        return poi != null && poi.IsNatural;                   // man-made → Grab
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => PickupVerbatim("gather", target);

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        if (target is not ItemElement itemElement) return System.Array.Empty<OutcomeReport>();
        return new[] { new ItemAcquisitionOutcome(itemElement) };
    }

    public override Item? AcquiredItem(Element? target) => (target as ItemElement)?.Item;

    // ── Routine recording ─────────────────────────────────────────────────────
    // Gathering is habitual foraging; the routine target is the item type (by ItemId), re-resolved in
    // a freshly built scene at replay. A depleted resource resolves to null → routine greys out.
    public override bool CanRecordAsRoutine(Scene scene, PoV pov, Element target, PartyMember actor)
        => target is ItemElement;

    public override RoutineTargetRef? RoutineTarget(Scene scene, PoV pov, Element target)
        => target is ItemElement item
            ? new RoutineTargetRef(RoutineTargetKind.Item, item.Item.ItemId, item.DisplayName)
            : null;

    // Gathering stays in place — no phase transition. As a routine's last step it ends in the outcome
    // popup (acquired items), then returns to travel.
    public override RoutinePhaseKind RoutineTriggeredPhase(Scene scene, PoV pov, Element target)
        => RoutinePhaseKind.None;
}
