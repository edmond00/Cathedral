using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Follows a <see cref="PathPointOfInterest"/> from the current area to the area on its far side.
/// Always passable from either end (paths have no lock state).
/// </summary>
public class FollowPathVerb : Verb
{
    public override string VerbId         => "follow_path";
    public override string DisplayName    => "Follow";
    public override int    BaseDifficulty => 1;

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (target is not PathPointOfInterest path) return false;
        return pov.Where.Id == path.AreaA.Id || pov.Where.Id == path.AreaB.Id;
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"follow {target.DisplayName.ToLowerInvariant()}";

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, Protagonist actor, Element target)
    {
        if (target is not PathPointOfInterest path) return System.Array.Empty<OutcomeReport>();
        var destination = path.Other(pov.Where);
        return new[] { new AreaMoveOutcome(destination) };
    }
}
