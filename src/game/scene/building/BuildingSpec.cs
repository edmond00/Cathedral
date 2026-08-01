using System;
using System.Collections.Generic;
using Cathedral.Fight;

namespace Cathedral.Game.Scene.Building;

/// <summary>Medieval building material that shapes descriptions.</summary>
public enum BuildingMaterial
{
    Wood,
    Stone,
    WattleAndDaub,
    Timber,
}

/// <summary>
/// Whether outsiders have business inside. Public buildings open onto a hall where the occupant
/// receives callers; private ones are shut to everyone but their household.
/// </summary>
public enum BuildingAccess { Public, Private }

/// <summary>Whether the building sleeps one person or a crew.</summary>
public enum BuildingOccupancy { Individual, Communal }

/// <summary>
/// The four buildings the two axes produce. Derived, never set directly — see
/// <see cref="BuildingSpec.Kind"/>.
/// </summary>
public enum BuildingKind
{
    /// <summary>Public + individual. A village workshop: the master works, trades and sleeps here.</summary>
    Workshop,
    /// <summary>Private + individual. A village house, e.g. an apprentice's.</summary>
    House,
    /// <summary>Public + communal. The main building of a farm or field: business hall plus a crew.</summary>
    Longhouse,
    /// <summary>Private + communal. Overflow sleeping quarters for farm or field hands.</summary>
    Barracks,
}

/// <summary>
/// What a room is for. Furniture, privacy and door wiring all dispatch on this rather than on the
/// display name — names are qualified per building ("Forge Kitchen"), so any name-matching would
/// break the moment a second building existed.
/// </summary>
public enum RoomRole
{
    PublicHall,
    Bedroom,
    Dormitory,
    Kitchen,
    Pantry,
    Store,
    Landing,
}

/// <summary>
/// Request for <see cref="BuildingFactory.Build"/>. The caller supplies identity, the two axes, the
/// outdoor area the entry door opens off, and how many sleepers must fit; everything else — floors,
/// room roster, layout, material, furniture, descriptions — is rolled.
/// </summary>
public sealed record BuildingSpec
{
    /// <summary>Section name, e.g. "Forge", "Longhouse", "Aldith's House".</summary>
    public required string BuildingName { get; init; }

    /// <summary>
    /// Qualifier prefixed to every room name ("Forge Bedroom"). Keeps room names unique across a
    /// location, which the narration graph requires: it keys nodes by a display-name slug, and a
    /// duplicate silently costs a room its node.
    /// </summary>
    public required string RoomPrefix { get; init; }

    public required BuildingAccess    Access    { get; init; }
    public required BuildingOccupancy Occupancy { get; init; }

    /// <summary>The outdoor area the entry door is reached from.</summary>
    public required Area OutsideArea { get; init; }

    /// <summary>
    /// Builds the room the entry door opens into. This is the reuse seam for the existing
    /// <c>WorkshopSubfactory</c> builders — a forge, mill or bakery floor becomes the public hall of
    /// its building, furniture and stock already in place. Null rolls a generic hall for the kind.
    /// </summary>
    public Func<Area>? PublicHallBuilder { get; init; }

    /// <summary>
    /// How many people sleep here. Driven by the caller's NPC roster, never the other way round: a
    /// building sized first and staffed from whatever beds it happened to roll leaves people
    /// homeless (or, as the old farm did, silently caps the roster at three).
    /// </summary>
    public int BedCount { get; init; } = 1;

    /// <summary>Forced material — pass one where the building's function demands it (a forge is stone).</summary>
    public BuildingMaterial? Material { get; init; }

    /// <summary>Arena flavour for fights inside. Defaults to <c>RoomsGenerator</c>.</summary>
    public Func<int, IFightAreaGenerator>? ArenaGeneratorFactory { get; init; }

    /// <summary>Extra descriptive noun for the building description ("forge", "farmhouse").</summary>
    public string? FunctionNoun { get; init; }

    /// <summary>
    /// Forces the weathering adjective in the rolled exterior description. Set it together with
    /// <see cref="BuildingName"/> when a building is known by that trait — "the crooked house" — so
    /// the name and the prose agree.
    /// </summary>
    public string? ExteriorWear { get; init; }

    public BuildingKind Kind => (Access, Occupancy) switch
    {
        (BuildingAccess.Public,  BuildingOccupancy.Individual) => BuildingKind.Workshop,
        (BuildingAccess.Private, BuildingOccupancy.Individual) => BuildingKind.House,
        (BuildingAccess.Public,  BuildingOccupancy.Communal)   => BuildingKind.Longhouse,
        _                                                      => BuildingKind.Barracks,
    };
}

/// <summary>
/// Result of <see cref="BuildingFactory.Build"/>. Everything is fully populated but NOT registered:
/// the caller passes this to <c>SceneFactory.RegisterBuilding</c>.
/// </summary>
public sealed record BuildingResult(
    Section                             Section,
    Area                                PublicHall,
    IReadOnlyList<Area>                 Bedrooms,
    IReadOnlyList<Area>                 AllRooms,
    DoorPointOfInterest                 EntryDoor,
    IReadOnlyList<DoorPointOfInterest>  InteriorDoors,
    IReadOnlyList<StairPointOfInterest> Stairs,
    IReadOnlyList<PointOfInterest>      Beds,
    BuildingMaterial                    Material,
    BuildingKind                        Kind,
    string                              BuildingDescription)
{
    /// <summary>
    /// One area per sleeper, in bed order — index i is where the i-th member of the caller's roster
    /// sleeps. A communal building repeats a dormitory as many times as it has beds in it.
    /// </summary>
    public IReadOnlyList<Area> BedAreas { get; init; } = Array.Empty<Area>();
}
