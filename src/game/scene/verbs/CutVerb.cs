using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc.Corpse;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Harvests a body-part item from a <see cref="CorpseBodyPartPoI"/> within a <see cref="CorpseSpot"/>.
/// The player must be inside the corpse spot for this verb to be active.
/// </summary>
public class CutVerb : Verb
{
    public override string VerbId         => "cut";
    public override string DisplayName    => "Cut";
    public override int    BaseDifficulty => 2;

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (target is not ItemElement) return false;
        if (pov.InSpot is not CorpseSpot) return false;

        // Item must be in a CorpseBodyPartPoI within the current spot
        return pov.InSpot.PointsOfInterest
            .OfType<CorpseBodyPartPoI>()
            .Any(poi => poi.Items.Any(ie => ie.Id == target.Id));
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
    {
        var name    = target.DisplayName.ToLowerInvariant();
        var article = "aeiou".Contains(name[0]) ? "an" : "a";
        return $"cut {article} {name}";
    }

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, Protagonist actor, Element target)
    {
        if (target is not ItemElement itemElement) return System.Array.Empty<OutcomeReport>();
        return new[] { new CorpseItemAcquisitionOutcome(itemElement) };
    }
}
