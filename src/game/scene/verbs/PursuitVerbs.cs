using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;
using Cathedral.Game.Npc;
using Cathedral.Game.Scene.Building;

using Cathedral.Game.Narrative.ModiMentis;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Reads an animal's sign and follows it to wherever the animal is now.
///
/// <para>A location's beasts move through the day, so at any hour most of their range is empty of
/// them. Tracking turns that from an absence into a route: the tracks tell you it was here, and
/// following them takes you to it. Without this a wolf is something you meet by walking into the
/// right area at the right hour, which is to say by luck.</para>
/// </summary>
public class TrackVerb : Verb
{

    public override string VerbId         => "track";
    public override string DisplayName    => "Track";
    public override int    BaseDifficulty => 3;

    /// <summary>Reading sign off the ground - perception, and so of a piece with the senses. What finds a trail is an eye and a memory for what a print means.</summary>
    public override ToolUsage ToolUse => ToolUsage.Excluded;

    /// <summary>A beast tracks by nose, a person by sign. Beast first: spoor_reading names a snout, and spoor_eye names eyes and anamnesis, which a beast also owns — so the other order would take the beast's own lesson away from it.</summary>
    public override IReadOnlyList<string> GrantedModusMentisIds(Element? target)
        => new[] { "spoor_reading", "spoor_eye" };

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
        => Destination(scene, pov, target) != null;

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"follow {DefiniteTarget(target)}";

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        var destination = Destination(scene, pov, target);
        return destination == null
            ? System.Array.Empty<Outcome>()
            : new Outcome[] { new AreaMoveOutcome(destination) };
    }

    /// <summary>
    /// Where the quarry is at this hour, or null when there is nothing to follow: the sign is not
    /// here, the animal is dead, it is absent from the whole location, or it is standing in front of
    /// you already.
    /// </summary>
    private static Area? Destination(Scene scene, PoV pov, Element target)
    {
        if (target is not FootprintPointOfInterest sign) return null;
        if (!pov.Where.PointsOfInterest.Contains(sign)) return null;
        if (!sign.Quarry.IsAlive) return null;

        var there = scene.GetAreaOf(sign.Quarry, pov.When);
        return there != null && there.Id != pov.Where.Id ? there : null;
    }

    /// <summary>Following an animal through country it chose takes you through the same country.</summary>
    public override IReadOnlyList<Wound?> FailurePenalties(Element? target) => new Wound?[]
    {
        null, null, null, null, new ContusionWound(),
    };

    // Not recordable: where the sign leads depends on the hour and on a schedule a replay re-rolls.

    public override RoutinePhaseKind RoutineTriggeredPhase(Scene scene, PoV pov, Element target)
        => RoutinePhaseKind.Narration;
}

/// <summary>
/// Follows a person, at a distance, until they go somewhere else — and arrives there when they do.
///
/// <para>Costs both time and position: waiting out the hours until they move, then coming up on the
/// nearest public ground to wherever they went. It stops at public ground deliberately. Following
/// someone through their own front door is a different crime, and the point of stalking is to learn
/// where a person goes without being in the room when they get there.</para>
///
/// <para>Illegal. Nobody follows a stranger around a village for a day innocently.</para>
/// </summary>
public class StalkVerb : Verb
{

    public override string VerbId         => "stalk";
    public override string DisplayName    => "Stalk";
    public override int    BaseDifficulty => 4;

    /// <summary>Moving unseen behind somebody. Nothing held makes a footfall quieter, and a full hand makes it louder.</summary>
    public override ToolUsage ToolUse => ToolUsage.Excluded;

    /// <summary>Following a stranger about for a day is a crime wherever it is done.</summary>
    protected override bool IsIllegalFor(Scene scene, PoV pov, Element? target, PartyMember? actor) => true;

    /// <summary>What a success teaches: staying with somebody who does not know you are there.</summary>
    public override string? GrantedModusMentisId(Element? target) => "stalking";

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
        => Plan(scene, pov, target) != null;

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"follow {NpcPronoun(target)} at a distance and see where {NpcSubjectPronoun(target)} goes";

    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbAction? view = null)
        => $"follow {NpcName(target)} to see where they go";

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        var plan = Plan(scene, pov, target);
        if (plan == null) return System.Array.Empty<Outcome>();

        var (period, arrival, npc) = plan.Value;
        return new Outcome[]
        {
            new TimeShiftOutcome(period),
            new AreaMoveOutcome(arrival),
            new NoticeOutcome(
                $"{npc.Entity.DisplayName} went to {scene.GetAreaOf(npc, period)?.DisplayName ?? "somewhere else"}",
                $"followed {npc.Entity.DisplayName} and saw where they went"),
        };
    }

    /// <summary>
    /// When the quarry next moves, and the nearest place you can stand and watch from. Null when
    /// there is nothing to follow — they are not here, they never move, or where they go has no
    /// public ground anywhere near it.
    /// </summary>
    private static (TimePeriod Period, Area Arrival, SceneNpc Npc)? Plan(Scene scene, PoV pov, Element target)
    {
        if (target is not SceneNpc sceneNpc) return null;
        if (sceneNpc.Entity is not NpcEntity npc || !npc.IsAlive || !npc.CanSpeak) return null;
        if (!scene.GetNpcsAt(pov.Where, pov.When).Any(n => n.Id == sceneNpc.Id)) return null;

        var period = scene.NextRelocation(sceneNpc, pov.When);
        if (period == null) return null;

        var destination = scene.GetAreaOf(sceneNpc, period.Value);
        if (destination == null) return null;

        var arrival = NearestPublic(scene, destination);
        return arrival == null ? null : (period.Value, arrival, sceneNpc);
    }

    /// <summary>
    /// The closest area to <paramref name="from"/> that is not private, walking outward over the area
    /// graph and any connector that is not a locked door.
    ///
    /// <para>Locked doors are excluded on purpose. Following somebody home should leave you outside
    /// the house, not inside it — getting in is what <c>unlock_door</c> and <c>slip_into</c> are
    /// for, and each of those is its own decision with its own witnesses.</para>
    /// </summary>
    private static Area? NearestPublic(Scene scene, Area from)
    {
        var seen  = new HashSet<System.Guid> { from.Id };
        var queue = new Queue<Area>();
        queue.Enqueue(from);

        while (queue.Count > 0)
        {
            var area = queue.Dequeue();
            if (!area.IsPrivate) return area;

            foreach (var next in Neighbours(scene, area))
                if (seen.Add(next.Id)) queue.Enqueue(next);
        }

        return null;
    }

    private static IEnumerable<Area> Neighbours(Scene scene, Area area)
    {
        if (scene.AreaGraph.TryGetValue(area.Id, out var reachable))
            foreach (var id in reachable)
                if (scene.GetArea(id) is { } neighbour) yield return neighbour;

        foreach (var connector in area.PointsOfInterest.OfType<ConnectorPointOfInterest>())
        {
            // A shut door is not a way through for somebody keeping out of sight.
            if (connector is DoorPointOfInterest door && door.DoorState == DoorState.Locked) continue;
            yield return connector.Other(area);
        }
    }

    // Not recordable: how long the wait is and where it ends both depend on the hour it started at.

    public override RoutinePhaseKind RoutineTriggeredPhase(Scene scene, PoV pov, Element target)
        => RoutinePhaseKind.Narration;
}
