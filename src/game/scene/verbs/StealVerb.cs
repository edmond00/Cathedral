using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc.Corpse;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Steals an item from a <see cref="Area.IsPrivate"/> area or spot.
/// Functionally identical to <see cref="GrabVerb"/> but marks the action as illegal,
/// which triggers witness detection and the "caught red-handed" dialogue on failure.
/// </summary>
public class StealVerb : Verb
{
    public override string VerbId         => "steal";
    public override string DisplayName    => "Steal";
    public override int    BaseDifficulty => 3;

    /// <summary>Stealing is always an illegal action.</summary>
    public override bool IsLegal => false;

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (target is not ItemElement itemEl) return false;

        if (pov.InSpot != null)
        {
            // Inside a spot of a private area only.
            if (!pov.Where.IsPrivate) return false;
            return pov.InSpot.PointsOfInterest
                .Where(poi => poi is not CorpseBodyPartPoI)
                .Any(poi => poi.Items.Any(ie => ie.Id == itemEl.Id));
        }
        else
        {
            // In a private area directly.
            if (!pov.Where.IsPrivate) return false;
            return pov.Where.PointsOfInterest
                .Where(poi => poi is not CorpseBodyPartPoI)
                .Any(poi => poi.Items.Any(ie => ie.Id == itemEl.Id));
        }
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
    {
        var name    = target.DisplayName.ToLowerInvariant();
        var article = "aeiou".Contains(name[0]) ? "an" : "a";
        return $"steal {article} {name}";
    }

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, Protagonist actor, Element target)
    {
        if (target is not ItemElement itemElement) return System.Array.Empty<OutcomeReport>();
        return new[] { new ItemAcquisitionOutcome(itemElement) };
    }
}
