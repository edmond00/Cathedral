using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Scene.Building;

using Cathedral.Game.Narrative.ModiMentis;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Follows a <see cref="PathPointOfInterest"/> from the current area to the area on its far side.
/// Always passable from either end (paths have no lock state).
/// </summary>
public class FollowPathVerb : Verb
{
    /// <summary>A worn lane is also an account of whose land it runs through.</summary>
    public override IEnumerable<ModusMentis> Lessons(LessonContext ctx)
    {
        if (ctx.Pov.Where is LaneArea or WalkArea or RowArea) yield return Mm<MarchstoneModusMentis>();
        // The target's own declaration, then this verb's default — always last, always visible.
        foreach (var m in base.Lessons(ctx)) yield return m;
    }
    public override string VerbId         => "follow_path";
    public override string DisplayName    => "Follow";
    public override int    BaseDifficulty => 1;

    /// <summary>Following a path that is already there. Walking is walking.</summary>
    public override ToolUsage ToolUse => ToolUsage.Excluded;

    /// <summary>What a success teaches: a path read and followed is terrain understood.</summary>
    /// <summary>
    /// What a success teaches: following a way that is worn rather than marked. <c>topographia</c>
    /// used to serve this, examining a landscape and asking about a district all at once, which made
    /// the most-walked verb in the game teach the same word as reading a horizon.
    /// </summary>
    public override string? GrantedModusMentisId(Element? target) => "trailcraft";

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
    {
        if (target is not PathPointOfInterest path) return false;
        return pov.Where.Id == path.AreaA.Id || pov.Where.Id == path.AreaB.Id;
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"follow {DefiniteTarget(target)}";

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        if (target is not PathPointOfInterest path) return System.Array.Empty<Outcome>();
        var destination = path.Other(pov.Where);
        return new[] { new AreaMoveOutcome(destination) };
    }
}
