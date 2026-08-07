using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc.Corpse;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Harvests a part from a body — a fang, a hide, a cut of meat — off a
/// <see cref="CorpsePointOfInterest"/> in the current area.
///
/// <para>The corpse's own type is the whole gate: everything a <see cref="CorpsePointOfInterest"/>
/// holds is flesh, so cut takes it and the pickup verbs do not, while everything in the plain
/// belongings PoI beside it is cloth and steel, so they take it and cut does not. Neither verb has to
/// look at an individual item to decide.</para>
/// </summary>
public class CutVerb : Verb
{
    public override string VerbId         => "cut";
    public override string DisplayName    => "Cut";
    public override int    BaseDifficulty => 2;

    /// <summary>What a success teaches: taking a body apart.</summary>
    public override string? GrantedModusMentisId(Element? target) => "butchery";

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (target is not ItemElement itemEl) return false;

        return pov.Where.PointsOfInterest
            .OfType<CorpsePointOfInterest>()
            .Any(corpse => corpse.Items.Any(ie => ie.Id == itemEl.Id));
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => PickupVerbatim("cut", target);

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        if (target is not ItemElement itemElement) return System.Array.Empty<OutcomeReport>();
        return new[] { new CorpseItemAcquisitionOutcome(itemElement) };
    }

    public override Item? AcquiredItem(Element? target) => (target as ItemElement)?.Item;
}
