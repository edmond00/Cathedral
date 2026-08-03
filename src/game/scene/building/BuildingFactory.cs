using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Fight.Generators;

namespace Cathedral.Game.Scene.Building;

/// <summary>
/// Procedural generator for a whole building — rooms, furniture, doors, stairs and prose — from a
/// <see cref="BuildingSpec"/>.
///
/// <para><b>One building is one <see cref="Section"/>.</b> Floors are joined by stairs inside that
/// section, never split across two. Sections must partition a scene's areas (every lookup is a
/// "first section containing this area"), and they are also the unit of ownership and of fight-ally
/// recruitment — so a per-floor split made a building's own occupants strangers to each other
/// upstairs and forced every caller to remember to register both halves.</para>
///
/// <para><b>Doors and stairs are the only interior topology.</b> Nothing here ever calls
/// <c>Scene.ConnectAreas</c>: an <see cref="Verbs.MoveToAreaVerb"/> edge would let the player walk
/// straight past a locked door, which is exactly what made the old village's one interior door
/// decorative.</para>
///
/// Lock rules, from the two axes:
/// <list type="bullet">
/// <item>Private — entry door locked, every interior door unlocked. The household is inside; getting
/// in at all is the barrier.</item>
/// <item>Public — entry door unlocked by day, opening onto a hall where callers are received; the
/// doors out of that hall into the rest of the building are locked. Stairs are placed behind that
/// boundary, never off the hall, so the upper floor is not a free way round it.</item>
/// <item>Every entry door is shut at night regardless — see <see cref="DoorPointOfInterest.EffectiveState"/>.</item>
/// </list>
/// </summary>
public static class BuildingFactory
{
    public static BuildingResult Build(BuildingSpec spec, Random rng)
    {
        var kind   = spec.Kind;
        var mat    = spec.Material ?? BuildingDescriptions.RollMaterial(kind, rng);
        int floors = RollFloors(kind, rng);
        int beds   = Math.Max(1, spec.BedCount);

        var allRooms   = new List<Area>();
        var groundInner = new List<(Area Room, RoomRole Role)>();   // ground rooms other than the hall
        var upperRooms  = new List<(Area Room, RoomRole Role)>();
        var sleeping    = new List<(Area Room, RoomRole Role, int Beds)>();

        // ── The hall ──────────────────────────────────────────────────────────
        // A workshop's hall is the existing shop-floor area (forge, mill, bakery …), kept under its
        // own name: it is already unique in the location and "Forge" reads better than "Forge Hall".
        var hall = spec.PublicHallBuilder?.Invoke()
                   ?? BuildingRooms.Build(RoomRole.PublicHall, spec.RoomPrefix, kind, mat, 1);
        allRooms.Add(hall);

        // ── Service rooms ─────────────────────────────────────────────────────
        foreach (var role in RollServiceRooms(kind, floors, rng))
        {
            var room = BuildingRooms.Build(role, spec.RoomPrefix, kind, mat, 1);
            groundInner.Add((room, role));
            allRooms.Add(room);
        }

        // ── Sleeping rooms ────────────────────────────────────────────────────
        // Sized from the caller's roster, not rolled: every occupant must have a bed of their own.
        foreach (var (role, count) in SplitBeds(spec.Occupancy, beds, rng))
        {
            int ordinal = sleeping.Count + 1;
            var room = BuildingRooms.Build(role, spec.RoomPrefix, kind, mat, ordinal);
            sleeping.Add((room, role, count));
            allRooms.Add(room);
        }

        // ── Floors ────────────────────────────────────────────────────────────
        Area? landing = null;
        if (floors >= 2)
        {
            landing = BuildingRooms.Build(RoomRole.Landing, spec.RoomPrefix, kind, mat, 1);
            allRooms.Add(landing);
            upperRooms.Add((landing, RoomRole.Landing));
            foreach (var s in sleeping) upperRooms.Add((s.Room, s.Role));
        }

        // ── Furniture ─────────────────────────────────────────────────────────
        // The hall is only furnished when we built it ourselves; a WorkshopSubfactory hall arrives
        // with its own anvil, millstone or oven already in place.
        if (spec.PublicHallBuilder == null)
            BuildingRooms.Populate(hall, RoomRole.PublicHall, mat, rng);
        foreach (var (room, role) in groundInner)
            BuildingRooms.Populate(room, role, mat, rng);
        foreach (var (room, role, count) in sleeping)
            BuildingRooms.Populate(room, role, mat, rng, count);

        // ── Privacy ───────────────────────────────────────────────────────────
        // A public hall is the one room outsiders may stand in without trespassing; everything else
        // is private, which is what turns GRAB into STEAL and a visit into intrusion.
        foreach (var room in allRooms)
            room.IsPrivate = true;
        if (spec.Access == BuildingAccess.Public)
            hall.IsPrivate = false;

        // ── Descriptions ──────────────────────────────────────────────────────
        var buildingDescription = BuildingDescriptions.RollBuildingDescription(
            kind, mat, floors, spec.FunctionNoun, rng, spec.ExteriorWear);

        // One phrase per door: the entry, one per service room, one per sleeping room. Sleeping rooms
        // hang off the landing upstairs or off the hall on a single storey — same count either way.
        int doorCount   = 1 + groundInner.Count + sleeping.Count;
        var doorPhrases = BuildingDescriptions.RollDoorDescriptions(mat, doorCount, rng);
        int phrase      = 0;

        // ── Doors ─────────────────────────────────────────────────────────────
        var entryState    = spec.Access == BuildingAccess.Public ? DoorState.Unlocked : DoorState.Locked;
        var fromHallState = spec.Access == BuildingAccess.Public ? DoorState.Locked   : DoorState.Unlocked;

        var interiorDoors = new List<DoorPointOfInterest>();
        var stairs        = new List<StairPointOfInterest>();

        var entryDoor = MakeDoor(
            spec.OutsideArea, hall,
            $"{spec.BuildingName} Door",
            DoorRole.Entry, entryState, doorPhrases[phrase++], buildingDescription,
            spec.BuildingName, phrase);

        foreach (var (room, _) in groundInner)
        {
            interiorDoors.Add(MakeDoor(
                hall, room, $"{room.DisplayName} Door",
                DoorRole.Interior, fromHallState, doorPhrases[phrase++], "",
                spec.BuildingName, phrase));
        }

        if (floors >= 2 && landing != null)
        {
            // Stairs sit behind the hall boundary in a public building: off an inner room if there is
            // one, so the upper floor cannot be reached without passing a locked door. RollServiceRooms
            // guarantees a public building has at least one inner room whenever it has two floors.
            var stairFoot = spec.Access == BuildingAccess.Public && groundInner.Count > 0
                ? groundInner[0].Room
                : hall;

            var stair = new StairPointOfInterest(
                stairFoot, landing, $"{spec.RoomPrefix} Staircase",
                new() { "A steep narrow staircase of rough-cut timber, worn smooth in the middle" })
            {
                StableKey = $"stair|{spec.BuildingName}|0",
            };
            stairFoot.PointsOfInterest.Add(stair);
            landing.PointsOfInterest.Add(stair);
            stairs.Add(stair);

            foreach (var (room, _) in upperRooms.Where(u => u.Role != RoomRole.Landing))
            {
                interiorDoors.Add(MakeDoor(
                    landing, room, $"{room.DisplayName} Door",
                    DoorRole.Interior, DoorState.Unlocked, doorPhrases[phrase++], "",
                    spec.BuildingName, phrase));
            }
        }
        else
        {
            foreach (var (room, _, _) in sleeping)
            {
                interiorDoors.Add(MakeDoor(
                    hall, room, $"{room.DisplayName} Door",
                    DoorRole.Interior, fromHallState, doorPhrases[phrase++], "",
                    spec.BuildingName, phrase));
            }
        }

        // ── Section ───────────────────────────────────────────────────────────
        var section = new Section(
            spec.BuildingName,
            new() { buildingDescription },
            spec.ArenaGeneratorFactory ?? (seed => new RoomsGenerator { Seed = seed })
        )
        { IsInterior = true };
        foreach (var room in allRooms) section.Areas.Add(room);

        // ── Beds, in roster order ─────────────────────────────────────────────
        var bedPois  = new List<PointOfInterest>();
        var bedAreas = new List<Area>();
        foreach (var (room, _, _) in sleeping)
        {
            foreach (var bed in BuildingRooms.BedsIn(room))
            {
                bedPois.Add(bed);
                bedAreas.Add(room);
            }
        }

        return new BuildingResult(
            Section:             section,
            PublicHall:          hall,
            Bedrooms:            sleeping.Select(s => s.Room).ToList(),
            AllRooms:            allRooms,
            EntryDoor:           entryDoor,
            InteriorDoors:       interiorDoors,
            Stairs:              stairs,
            Beds:                bedPois,
            Material:            mat,
            Kind:                kind,
            BuildingDescription: buildingDescription)
        {
            BedAreas = bedAreas,
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a door, attaches it to both areas, and pre-assigns its stable key.
    ///
    /// <para>The key is set here rather than left to <c>SceneFactory.AssignStableKeys</c> because a
    /// door lives in two areas' PoI lists: the keying walk would visit it twice and the key would end
    /// up depending on which side was walked last, so the door's rolled description would not survive
    /// a rebuild.</para>
    /// </summary>
    private static DoorPointOfInterest MakeDoor(
        Area front, Area back, string name, DoorRole role, DoorState state,
        string doorPhrase, string buildingPhrase, string buildingKey, int ordinal)
    {
        var door = new DoorPointOfInterest(front, back, name, new() { doorPhrase }, state)
        {
            Role                = role,
            DoorDescription     = doorPhrase,
            BuildingDescription = buildingPhrase,
            StableKey           = $"door|{buildingKey}|{ordinal}",
        };
        front.PointsOfInterest.Add(door);
        back.PointsOfInterest.Add(door);
        return door;
    }

    private static int RollFloors(BuildingKind kind, Random rng) => kind switch
    {
        BuildingKind.Workshop  => rng.NextDouble() < 0.55 ? 2 : 1,   // quarters over the shop
        BuildingKind.House     => rng.NextDouble() < 0.40 ? 2 : 1,
        BuildingKind.Longhouse => rng.NextDouble() < 0.30 ? 2 : 1,   // mostly a single long room
        _                      => 1,                                  // barracks are always one storey
    };

    /// <summary>
    /// The non-hall ground rooms. A public building with two floors always gets at least one, because
    /// its staircase has to stand behind the hall's locked boundary.
    /// </summary>
    private static List<RoomRole> RollServiceRooms(BuildingKind kind, int floors, Random rng)
    {
        var rooms = new List<RoomRole>();

        switch (kind)
        {
            case BuildingKind.Workshop:
                rooms.Add(RoomRole.Store);
                if (rng.NextDouble() < 0.50) rooms.Add(RoomRole.Kitchen);
                if (rng.NextDouble() < 0.35) rooms.Add(RoomRole.Pantry);
                break;

            case BuildingKind.House:
                rooms.Add(RoomRole.Kitchen);
                if (rng.NextDouble() < 0.60) rooms.Add(RoomRole.Pantry);
                if (rng.NextDouble() < 0.30) rooms.Add(RoomRole.Store);
                break;

            case BuildingKind.Longhouse:
                rooms.Add(RoomRole.Kitchen);
                if (rng.NextDouble() < 0.70) rooms.Add(RoomRole.Pantry);
                if (rng.NextDouble() < 0.60) rooms.Add(RoomRole.Store);
                break;

            default: // Barracks — bunks and somewhere to drop kit
                if (rng.NextDouble() < 0.50) rooms.Add(RoomRole.Store);
                break;
        }

        if (floors >= 2 && rooms.Count == 0) rooms.Add(RoomRole.Store);
        return rooms;
    }

    /// <summary>
    /// Splits <paramref name="beds"/> across sleeping rooms. An individual building gets one bedroom
    /// with one bed; a communal one spreads its beds over up to three dormitories so a big crew is
    /// not all in one row.
    /// </summary>
    private static List<(RoomRole Role, int Beds)> SplitBeds(BuildingOccupancy occupancy, int beds, Random rng)
    {
        if (occupancy == BuildingOccupancy.Individual)
            return new() { (RoomRole.Bedroom, 1) };

        int rooms  = Math.Max(1, Math.Min(beds, rng.Next(1, 4)));
        var result = new List<(RoomRole, int)>();
        int left   = beds;
        for (int i = 0; i < rooms; i++)
        {
            int share = i == rooms - 1 ? left : Math.Max(1, left / (rooms - i));
            result.Add((RoomRole.Dormitory, share));
            left -= share;
            if (left <= 0) break;
        }
        return result;
    }
}
