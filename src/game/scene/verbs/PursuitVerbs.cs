using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
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
    public override IReadOnlyList<FailurePenalty> FailurePenalties(Element? target) => new FailurePenalty[]
    {
        FailurePenalty.GetLost, FailurePenalty.GetLost, FailurePenalty.WasteTime, FailurePenalty.LoseFocus, new ContusionWound(),
    };
}

/// <summary>
/// Follows a person, at a distance, until they go somewhere else — and arrives there when they do.
///
/// <para>Costs both time and position: waiting out the hours until they move, then following them
/// there — all the way, or up to the first door they lock behind them. Going through that door is a
/// different crime, and its own act; the stalk says where they went and stops in front of it.</para>
///
/// <para>Illegal. Nobody follows a stranger around a village for a day innocently.</para>
/// </summary>
public class StalkVerb : Verb
{

    public override string VerbId         => "stalk";
    public override string DisplayName    => "Stalk";

    /// <summary>Following somebody who does not know it, and losing them, and then yourself.</summary>
    public override IReadOnlyList<FailurePenalty> FailurePenalties(Element? target) => new FailurePenalty[]
    {
        FailurePenalty.GetLost, FailurePenalty.GetLost, FailurePenalty.WasteTime, FailurePenalty.LoseFocus,
    };
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

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        var plan = Plan(scene, pov, target);
        if (plan == null) return System.Array.Empty<Outcome>();

        var (period, arrival, npc, destination, door) = plan.Value;
        var label = Cathedral.Game.Npc.NpcLabelResolver.ShortLabel(npc.Entity, actor);
        var Label = Cathedral.Game.Npc.NpcLabelResolver.ShortLabelCapitalized(npc.Entity, actor);

        if (door == null)
            return new Outcome[]
            {
                new TimeShiftOutcome(period),
                new AreaMoveOutcome(arrival),
                new NoticeOutcome($"{Label} went to {destination.DisplayName}",
                                  $"followed {label} and saw where they went"),
            };

        // A locked door was in the way: say so, and the next phase looks at it.
        return new Outcome[]
        {
            new TimeShiftOutcome(period),
            new AreaMoveOutcome(arrival),
            new QuarryBeyondOutcome(door,
                $"{Label} went behind {door.DisplayName} (locked)",
                $"followed {label} until {NpcSubjectPronoun(target)} went behind {NeutralNarration.ItemPhrase(door.DisplayName)} "
                + $"and locked it, and I could follow no further"),
        };
    }

    /// <summary>
    /// When the quarry next moves, where they go, and how far the follower gets: all the way, or to
    /// the near side of the first door locked against them on the way there, when there is one.
    /// Null when there is nothing to follow — they are not here, they never move, or nothing joins
    /// the two places at all.
    /// </summary>
    private static (TimePeriod Period, Area Arrival, SceneNpc Npc, Area Destination, DoorPointOfInterest? Door)?
        Plan(Scene scene, PoV pov, Element target)
    {
        if (target is not SceneNpc sceneNpc) return null;
        if (sceneNpc.Entity is not NpcEntity npc || !npc.IsAlive || !npc.CanSpeak) return null;
        if (!scene.GetNpcsAt(pov.Where, pov.When).Any(n => n.Id == sceneNpc.Id)) return null;

        var period = scene.NextRelocation(sceneNpc, pov.When);
        if (period == null) return null;

        var destination = scene.GetAreaOf(sceneNpc, period.Value);
        if (destination == null) return null;

        // A way with no lock on it, however long, is followed to the end.
        if (Way(scene, pov.Where, destination, lockedAt: period.Value) != null)
            return (period.Value, destination, sceneNpc, destination, null);

        var way = Way(scene, pov.Where, destination, lockedAt: null);
        if (way == null) return null;

        // Walked as far as the first door shut against the follower — locked from the side they come
        // to it by. Private ground behind it is somebody's, and getting in is what unlock_door and
        // slip_into are for: each its own decision, with its own witnesses. Every private area is
        // behind such a door (BuildingAudit.CheckPrivateBehindLocks), so a follower who starts on
        // public ground ends on it; one who starts inside a house may follow through its rooms.
        var at = pov.Where;
        foreach (var (next, by) in way)
        {
            if (LockedAgainst(by, at, period.Value))
                return (period.Value, at, sceneNpc, destination, (DoorPointOfInterest)by);
            at = next;
        }
        return (period.Value, destination, sceneNpc, destination, null);
    }

    /// <summary>
    /// The shortest way from <paramref name="from"/> to <paramref name="to"/> over every way there is
    /// (open ground and every connector), as the area reached at each step and what it was reached by.
    /// With <paramref name="lockedAt"/>, no door locked from the side it is come to at that hour. Empty
    /// when the two are one; null when nothing joins them.
    /// </summary>
    private static List<(Area Next, Element By)>? Way(Scene scene, Area from, Area to, TimePeriod? lockedAt)
    {
        var cameBy = new Dictionary<System.Guid, (Area From, Element By)> { [from.Id] = (from, from) };
        var queue  = new Queue<Area>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            var area = queue.Dequeue();
            if (area.Id == to.Id)
            {
                var steps = new List<(Area, Element)>();
                for (var a = area; a.Id != from.Id; a = cameBy[a.Id].From)
                    steps.Add((a, cameBy[a.Id].By));
                steps.Reverse();
                return steps;
            }
            foreach (var (next, by) in AllWays(scene, area))
            {
                if (lockedAt is { } when && LockedAgainst(by, area, when)) continue;
                if (cameBy.TryAdd(next.Id, (area, by))) queue.Enqueue(next);
            }
        }
        return null;
    }

    /// <summary>A door locked at <paramref name="when"/> from the side <paramref name="from"/> comes to it by.</summary>
    private static bool LockedAgainst(Element by, Area from, TimePeriod when)
        => by is DoorPointOfInterest door && from.Id != door.BackArea.Id
           && door.EffectiveState(when) == DoorState.Locked;

    private static IEnumerable<(Area Next, Element By)> AllWays(Scene scene, Area area)
    {
        if (scene.AreaGraph.TryGetValue(area.Id, out var reachable))
            foreach (var id in reachable)
                if (scene.GetArea(id) is { } neighbour) yield return (neighbour, neighbour);
        foreach (var connector in area.PointsOfInterest.OfType<ConnectorPointOfInterest>())
            if (connector is not LandscapePointOfInterest && connector.Touches(area))
                yield return (connector.Other(area), connector);
    }
}
