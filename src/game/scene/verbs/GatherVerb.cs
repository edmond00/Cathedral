using System.Collections.Generic;
using Cathedral.Game.Narrative;

using Cathedral.Game.Narrative.ModiMentis;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Gathers a <b>natural/wild</b> resource item (from a <see cref="PointOfInterest.IsNatural"/> PoI)
/// in the current area or spot. The natural counterpart of <see cref="GrabVerb"/>, and the only
/// pickup verb that teaches a gathering routine (so "gather mushrooms here" can be gone back to).
/// Depletion + regeneration apply to all picked items; natural resources regrow faster (see
/// <c>ResourceRegen</c>).
/// </summary>
public class GatherVerb : Verb
{
    /// <summary>What is being gathered, which the patch does not usually declare.</summary>
    public override IEnumerable<ModusMentis> Lessons(LessonContext ctx)
    {
        if (ctx.Holder is HerbPointOfInterest) yield return Mm<SimplingModusMentis>();
        if (ctx.Holder is BushPointOfInterest) yield return Mm<BerryingModusMentis>();
        // The target's own declaration, then this verb's default — always last, always visible.
        foreach (var m in base.Lessons(ctx)) yield return m;
    }
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

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        if (target is not ItemElement itemElement) return System.Array.Empty<Outcome>();
        return new[] { new ItemAcquisitionOutcome(itemElement) };
    }

    public override Item? AcquiredItem(Element? target) => (target as ItemElement)?.Item;
}
