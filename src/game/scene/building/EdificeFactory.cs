using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Fight;
using Cathedral.Fight.Generators;

namespace Cathedral.Game.Scene.Building;

/// <summary>What a room of a great building is for. Dispatches furniture and naming, never the display name.</summary>
public enum EdificeRoom
{
    Entrance, Corridor, GreatHall, Refectory, Kitchen, Larder, Scullery, Bathhouse, Latrine, Laundry,
    Dormitory, Cell, Chamber, Solar, Chapel, Crypt, Library, Scriptorium, Archive, Classroom, Lecture,
    Armoury, Guardroom, Dungeon, Treasury, Countinghouse, Chancery, Infirmary, Cellar, Store,
    Brewhouse, Wardrobe, Workshop, Observatory, Battlements,
}

/// <summary>Which storey a requested room belongs on.</summary>
public enum EdificeLevel { Ground, Upper, Top, Below, Any }

/// <summary>A room a great building must contain: what, how many, and on which storey.</summary>
public sealed record EdificeRoomRequest(EdificeRoom Role, int Count = 1, EdificeLevel Level = EdificeLevel.Any);

/// <summary>How the residents of a great building sleep.</summary>
public enum SleepStyle
{
    /// <summary>In shared dormitories — a garrison, a school, a household of servants.</summary>
    Dormitories,
    /// <summary>Each in a cell of their own — a monastery.</summary>
    Cells,
}

/// <summary>Request for <see cref="EdificeFactory.Build"/>: identity, programme, residents, style.</summary>
public sealed record EdificeSpec
{
    /// <summary>The section name: "the castle of Varsk", "the Monastery of the Closed Eye".</summary>
    public required string Name { get; init; }

    /// <summary>Prefixed to every room name, which must be unique across the location.</summary>
    public required string Prefix { get; init; }

    /// <summary>The outdoor area its great door opens from.</summary>
    public required Area Approach { get; init; }

    public required BuildingMaterial Material { get; init; }

    /// <summary>What the building is, for its exterior description ("castle", "monastery").</summary>
    public required string FunctionNoun { get; init; }

    /// <summary>The rooms it must have. Furniture comes from <see cref="Furnish"/>, then the defaults.</summary>
    public required IReadOnlyList<EdificeRoomRequest> Rooms { get; init; }

    /// <summary>Rooms it may be filled out with until it reaches its size, drawn at random.</summary>
    public IReadOnlyList<EdificeRoom> Filler { get; init; } = new[] { EdificeRoom.Store, EdificeRoom.Chamber, EdificeRoom.Cellar };

    /// <summary>How many people live in it: one bed each, laid out by <see cref="Sleep"/>.</summary>
    public int Residents { get; init; } = 8;

    public SleepStyle Sleep { get; init; } = SleepStyle.Dormitories;

    public int MinFloors { get; init; } = 3;
    public int MaxFloors { get; init; } = 6;

    /// <summary>The total rooms to reach, corridors included; filler makes up the difference.</summary>
    public int MinRooms { get; init; } = 30;
    public int MaxRooms { get; init; } = 50;

    /// <summary>Whether the building has a storey below ground.</summary>
    public bool HasUndercroft { get; init; } = true;

    /// <summary>Rooms an outsider may stand in without trespassing. The entrance always is.</summary>
    public IReadOnlySet<EdificeRoom> PublicRooms { get; init; } = new HashSet<EdificeRoom>();

    /// <summary>Rooms whose door is kept locked.</summary>
    public IReadOnlySet<EdificeRoom> LockedRooms { get; init; } = new HashSet<EdificeRoom> { EdificeRoom.Treasury, EdificeRoom.Armoury, EdificeRoom.Dungeon };

    /// <summary>
    /// The factory's own furnishing for a room, given its role and the building's rng. Return true when
    /// it furnished the room; false falls through to <see cref="EdificeRooms"/>' defaults.
    /// </summary>
    public Func<EdificeRoom, Area, Random, bool>? Furnish { get; init; }

    /// <summary>The word a storey's passage is called by ("Gallery", "Passage", "Walk").</summary>
    public string PassageWord { get; init; } = "Passage";

    public Func<int, IFightAreaGenerator>? Arena { get; init; }
}

/// <summary>What <see cref="EdificeFactory.Build"/> made. Populated, not yet registered.</summary>
public sealed class EdificeResult
{
    public required Section Section { get; init; }
    public required Area Entrance { get; init; }
    public required Area Battlements { get; init; }
    public required DoorPointOfInterest EntryDoor { get; init; }
    public required IReadOnlyList<Area> Rooms { get; init; }
    public required IReadOnlyDictionary<EdificeRoom, List<Area>> ByRole { get; init; }

    /// <summary>One area per resident, in roster order: where the i-th resident sleeps.</summary>
    public required IReadOnlyList<Area> BedAreas { get; init; }

    /// <summary>Rooms of the given roles, in build order.</summary>
    public IReadOnlyList<Area> Of(params EdificeRoom[] roles)
        => roles.SelectMany(r => ByRole.TryGetValue(r, out var l) ? l : new List<Area>()).ToList();

    public Area? First(EdificeRoom role) => ByRole.TryGetValue(role, out var l) && l.Count > 0 ? l[0] : null;
}

/// <summary>
/// Builds a <b>great building</b> — a castle keep, a monastery, a temple, a palace, a school, a fort:
/// three to six storeys and thirty to fifty rooms, enough that the people living in it can spend the
/// whole day inside. Every one of them sleeps in a bed of their own, eats in its refectory or hall,
/// washes in its bathhouse and works in its rooms.
///
/// <para><b>Shape.</b> Each storey is a passage with rooms off it; stairs join the passages; the
/// ground passage is the entrance, reached from outside by the great door; a storey below ground
/// (cellars, crypt, dungeon) hangs under it when <see cref="EdificeSpec.HasUndercroft"/>; the
/// battlements sit on top, up a last stair, and are the building's lookout. Like a small building it
/// is <b>one section</b>, and doors and stairs are its only topology — no area-graph edge inside, so
/// a locked door is a locked door.</para>
///
/// <para><b>Programme.</b> The caller names the rooms the building must have and where, and may list
/// filler to make up its size; sleeping rooms are added for its residents. The caller furnishes what
/// is particular to it through <see cref="EdificeSpec.Furnish"/> and <see cref="EdificeRooms"/> does
/// the rest, so a castle's armoury and a temple's sanctum differ while their kitchens need no
/// authoring at all.</para>
/// </summary>
public static class EdificeFactory
{
    public static EdificeResult Build(EdificeSpec spec, Random rng)
    {
        int floors = rng.Next(spec.MinFloors, spec.MaxFloors + 1);
        int target = rng.Next(spec.MinRooms, spec.MaxRooms + 1);
        var mat = spec.Material;

        // ── Storeys and their passages ────────────────────────────────────────
        // Level -1 is the undercroft, 0 the ground (whose passage is the entrance), then 1..floors-1.
        var levels = new List<int>();
        if (spec.HasUndercroft) levels.Add(-1);
        for (int f = 0; f < floors; f++) levels.Add(f);

        var passages = new Dictionary<int, Area>();
        var roomsAt = levels.ToDictionary(l => l, _ => new List<(Area Room, EdificeRoom Role)>());
        var byRole = new Dictionary<EdificeRoom, List<Area>>();
        var all = new List<Area>();

        void Track(Area a, EdificeRoom role)
        {
            if (!byRole.TryGetValue(role, out var list)) byRole[role] = list = new List<Area>();
            list.Add(a);
            all.Add(a);
        }

        foreach (int l in levels)
        {
            var role = l == 0 ? EdificeRoom.Entrance : EdificeRoom.Corridor;
            var passage = EdificeRooms.Room(role, spec, LevelName(l, floors), mat);
            passages[l] = passage;
            Track(passage, role);
        }

        int Pick(EdificeLevel level) => level switch
        {
            EdificeLevel.Ground => 0,
            EdificeLevel.Below  => spec.HasUndercroft ? -1 : 0,
            EdificeLevel.Top    => floors - 1,
            EdificeLevel.Upper  => floors > 1 ? rng.Next(1, floors) : 0,
            _                   => levels[rng.Next(levels.Count)],
        };

        var counters = new Dictionary<EdificeRoom, int>();
        Area Make(EdificeRoom role, int level, string? qualifier = null)
        {
            counters[role] = counters.GetValueOrDefault(role) + 1;
            var room = EdificeRooms.Room(role, spec, qualifier ?? EdificeRooms.Ordinal(counters[role]), mat);
            roomsAt[level].Add((room, role));
            Track(room, role);
            return room;
        }

        // ── The required programme ────────────────────────────────────────────
        foreach (var request in spec.Rooms)
            for (int i = 0; i < request.Count; i++)
                Make(request.Role, Pick(request.Level));

        // ── Sleeping rooms: one bed per resident ─────────────────────────────
        var sleepRooms = new List<(Area Room, int Beds)>();
        int residents = Math.Max(1, spec.Residents);
        var upper = levels.Where(l => l >= 1).DefaultIfEmpty(0).ToList();
        if (spec.Sleep == SleepStyle.Cells)
        {
            for (int i = 0; i < residents; i++)
                sleepRooms.Add((Make(EdificeRoom.Cell, upper[rng.Next(upper.Count)]), 1));
        }
        else
        {
            int left = residents;
            while (left > 0)
            {
                int share = Math.Min(left, rng.Next(3, 7));
                sleepRooms.Add((Make(EdificeRoom.Dormitory, upper[rng.Next(upper.Count)]), share));
                left -= share;
            }
        }

        // ── Filler to size ────────────────────────────────────────────────────
        while (all.Count < target && spec.Filler.Count > 0)
        {
            var role = spec.Filler[rng.Next(spec.Filler.Count)];
            int level = role is EdificeRoom.Cellar or EdificeRoom.Crypt or EdificeRoom.Dungeon && spec.HasUndercroft
                ? -1
                : levels.Where(l => l >= 0).ElementAt(rng.Next(floors));
            Make(role, level);
        }

        // ── Battlements: the lookout, up a last stair from the top storey ────
        var battlements = EdificeRooms.Room(EdificeRoom.Battlements, spec, "", mat);
        Track(battlements, EdificeRoom.Battlements);

        // ── Furnishing ────────────────────────────────────────────────────────
        var beds = new List<Area>();
        foreach (int l in levels)
            foreach (var (room, role) in roomsAt[l])
            {
                if (role is EdificeRoom.Dormitory or EdificeRoom.Cell) continue;
                if (spec.Furnish?.Invoke(role, room, rng) != true) EdificeRooms.Populate(role, room, mat, rng);
            }
        foreach (var (room, count) in sleepRooms)
        {
            EdificeRooms.PopulateSleeping(room, count, rng);
            for (int b = 0; b < count; b++) beds.Add(room);
        }
        foreach (var (l, passage) in passages)
            if (spec.Furnish?.Invoke(l == 0 ? EdificeRoom.Entrance : EdificeRoom.Corridor, passage, rng) != true)
                EdificeRooms.Populate(l == 0 ? EdificeRoom.Entrance : EdificeRoom.Corridor, passage, mat, rng);
        if (spec.Furnish?.Invoke(EdificeRoom.Battlements, battlements, rng) != true)
            EdificeRooms.Populate(EdificeRoom.Battlements, battlements, mat, rng);

        // ── Privacy ───────────────────────────────────────────────────────────
        foreach (var room in all) room.IsPrivate = true;
        passages[0].IsPrivate = false;
        foreach (var role in spec.PublicRooms)
            if (byRole.TryGetValue(role, out var open)) foreach (var a in open) a.IsPrivate = false;
        battlements.IsPrivate = false;

        // ── Doors and stairs ──────────────────────────────────────────────────
        int doorTotal = 1 + all.Count;
        var phrases = BuildingDescriptions.RollDoorDescriptions(mat, doorTotal, rng);
        int phrase = 0, ordinal = 0;
        string key = spec.Name;

        var description = $"{BuildingDescriptions.RollBuildingDescription(BuildingKind.Longhouse, mat, 2, spec.FunctionNoun, rng)}, "
                        + $"{NumberWord(floors)} storeys high";

        var entry = Door(spec.Approach, passages[0], $"{spec.Prefix} Great Door", DoorRole.Entry, DoorState.Unlocked,
                         phrases[phrase++], description, key, ++ordinal);

        foreach (int l in levels)
            foreach (var (room, role) in roomsAt[l])
            {
                var state = spec.LockedRooms.Contains(role) ? DoorState.Locked : DoorState.Unlocked;
                Door(passages[l], room, $"{room.DisplayName} Door", DoorRole.Interior, state,
                     phrases[Math.Min(phrase++, phrases.Length - 1)], "", key, ++ordinal);
            }

        // Stairs join each storey's passage to the next one up, the undercroft included.
        var ordered = levels.OrderBy(l => l).ToList();
        for (int i = 0; i + 1 < ordered.Count; i++)
            Stair(passages[ordered[i]], passages[ordered[i + 1]],
                  ordered[i] < 0 ? $"{spec.Prefix} Undercroft Stair" : $"{spec.Prefix} {LevelName(ordered[i + 1], floors)} Stair",
                  key, i);
        Stair(passages[floors - 1], battlements, $"{spec.Prefix} Tower Stair", key, ordered.Count);

        // ── Section ───────────────────────────────────────────────────────────
        var section = new Section(spec.Name, new() { description },
                                  spec.Arena ?? (seed => new RoomsGenerator { Seed = seed })) { IsInterior = true };
        section.Areas.AddRange(all);

        return new EdificeResult
        {
            Section = section,
            Entrance = passages[0],
            Battlements = battlements,
            EntryDoor = entry,
            Rooms = all,
            ByRole = byRole,
            BedAreas = beds,
        };
    }

    private static string LevelName(int level, int floors) => level switch
    {
        -1 => "Undercroft",
        0  => "Ground",
        _ when level == floors - 1 && floors > 2 => "Upper",
        1  => "First Floor",
        2  => "Second Floor",
        3  => "Third Floor",
        4  => "Fourth Floor",
        _  => $"Floor {level}",
    };

    private static string NumberWord(int n) => n switch
    {
        1 => "one", 2 => "two", 3 => "three", 4 => "four", 5 => "five", 6 => "six", _ => n.ToString(),
    };

    private static DoorPointOfInterest Door(Area front, Area back, string name, DoorRole role, DoorState state,
                                            string phrase, string buildingPhrase, string key, int ordinal)
    {
        var door = new DoorPointOfInterest(front, back, name, new() { phrase }, state)
        {
            Role = role,
            DoorDescription = phrase,
            BuildingDescription = buildingPhrase,
            StableKey = $"door|{key}|{ordinal}",
        };
        front.PointsOfInterest.Add(door);
        back.PointsOfInterest.Add(door);
        return door;
    }

    private static void Stair(Area bottom, Area top, string name, string key, int ordinal)
    {
        var stair = new StairPointOfInterest(bottom, top, name,
            new() { "A turning stair of worn stone, each tread dished in the middle by centuries of feet" })
        {
            StableKey = $"stair|{key}|{ordinal}",
        };
        bottom.PointsOfInterest.Add(stair);
        top.PointsOfInterest.Add(stair);
    }
}
