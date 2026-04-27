using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc.Corpse;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Picks up an item from a <see cref="PointOfInterest"/> in the current area or current spot.
/// Does not apply to <see cref="CorpseBodyPartPoI"/> items — use <see cref="CutVerb"/> for those.
/// </summary>
public class GrabVerb : Verb
{
    public override string VerbId         => "grab";
    public override string DisplayName    => "Grab";
    public override int    BaseDifficulty => 1;

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (target is not ItemElement itemEl) return false;

        if (pov.InSpot != null)
        {
            // Inside a spot: item must be in a non-corpse PoI of the current spot.
            // Private areas require StealVerb instead.
            if (pov.Where.IsPrivate) return false;
            return pov.InSpot.PointsOfInterest
                .Where(poi => poi is not CorpseBodyPartPoI)
                .Any(poi => poi.Items.Any(ie => ie.Id == itemEl.Id));
        }
        else
        {
            // In an area: item must be in a non-corpse PoI of the current area.
            // Private areas require StealVerb instead.
            if (pov.Where.IsPrivate) return false;
            return pov.Where.PointsOfInterest
                .Where(poi => poi is not CorpseBodyPartPoI)
                .Any(poi => poi.Items.Any(ie => ie.Id == itemEl.Id));
        }
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
    {
        var name    = target.DisplayName.ToLowerInvariant();
        var article = "aeiou".Contains(name[0]) ? "an" : "a";
        return $"grab {article} {name}";
    }

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, Protagonist actor, Element target)
    {
        if (target is not ItemElement itemElement) return System.Array.Empty<OutcomeReport>();
        return new[] { new ItemAcquisitionOutcome(itemElement) };
    }
}
