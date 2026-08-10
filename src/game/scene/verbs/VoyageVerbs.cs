using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Sets out for somewhere you can see from up here, straight across country.
///
/// <para>The one verb that moves between areas without walking a path. That is deliberate and it is
/// earned: it is offered only on a <see cref="LandscapePointOfInterest"/>, which hangs only in an
/// area you had to climb to — so the price of the shortcut is the climb, and the roll it cost.</para>
///
/// <para><b>There is no looking step.</b> This replaced a pair where one verb searched a horizon to
/// record what could be seen and a second walked to it. The searching was the narration system's job
/// all along — you observe a landscape the way you observe anything else, and the observation offers
/// the journey. What the old pair bought for its extra roll was a set of area ids on the point of
/// view, which nothing else in the game gated on and which the verb refresh could not see.</para>
/// </summary>
public class VoyageTowardVerb : Verb
{
    public override string VerbId         => "voyage_toward";
    public override string DisplayName    => "Voyage Toward";
    public override int    BaseDifficulty => 2;

    /// <summary>What a success teaches: holding a bearing across ground you have only seen from afar.</summary>
    public override string? GrantedModusMentisId(Element? target) => "voyage";

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
        => target is LandscapePointOfInterest landscape
        && pov.Where.Id == landscape.Viewpoint.Id
        && pov.Where.Id != landscape.Destination.Id;

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => target is LandscapePointOfInterest landscape
            ? $"set out for the {landscape.Destination.DisplayName.ToLowerInvariant()}"
            : "set out for what I can see from here";

    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbAction? view = null)
        => Verbatim(scene, pov, target);

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
        => target is LandscapePointOfInterest landscape
            ? new Outcome[] { new AreaMoveOutcome(landscape.Destination) }
            : System.Array.Empty<Outcome>();

    /// <summary>Cross-country is rougher going than a path. Rarely, it turns an ankle.</summary>
    public override IReadOnlyList<Wound?> FailurePenalties(Element? target) => new Wound?[]
    {
        null, null, null, null, new AnkleFractureRightWound(),
    };

    // ── Routine recording ─────────────────────────────────────────────────────
    // Recordable, unlike the verb it replaces. That one depended on landmarks revealed this visit,
    // which a replay does not have; a landscape is scene furniture the rebuild puts back, so a
    // journey learned once can be walked again.

    public override bool CanRecordAsRoutine(Scene scene, PoV pov, Element target, PartyMember actor)
        => target is LandscapePointOfInterest;

    public override RoutineTargetRef? RoutineTarget(Scene scene, PoV pov, Element target)
        => target is PointOfInterest poi
            ? new RoutineTargetRef(RoutineTargetKind.PointOfInterest, poi.ReferenceLemma, poi.DisplayName)
            : null;

    public override RoutinePhaseKind RoutineTriggeredPhase(Scene scene, PoV pov, Element target)
        => RoutinePhaseKind.Narration;
}
