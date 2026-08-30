using System.Collections.Generic;

namespace Cathedral.Game.Scene;

// ─────────────────────────────────────────────────────────────────────────────
//  What an area IS, as a type rather than as a string — the same move already made for points of
//  interest in PoiKinds.cs, and made here for the same reason.
//
//  An area kind used to be a `referenceLemma: "alehouse"` written at the construction site and read
//  back by lesson conditions through a substring match on the name. Nothing connected the two ends,
//  so a condition could name a kind of room that was never built and nothing noticed — and the
//  substring match went wrong in both directions besides, "cross" matching "Crossing" and "barrow"
//  matching "narrow".
//
//  As types that is a compile error, and reflection can ask the other half: which kinds of room does
//  no factory ever build?
//
//  The lemma is DERIVED from the class name, so the two cannot drift. The display name stays a
//  per-site argument: the kind is what a room is, the name is what this one is called — "Mill Lane"
//  and "Back Lane" are both LaneArea.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>A alcove.</summary>
public class AlcoveArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "alcove";

    public AlcoveArea(string displayName, string contextDescription, string transitionDescription,
                      List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A alehouse.</summary>
public class AlehouseArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "alehouse";

    public AlehouseArea(string displayName, string contextDescription, string transitionDescription,
                        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A bakery.</summary>
public class BakeryArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "bakery";

    public BakeryArea(string displayName, string contextDescription, string transitionDescription,
                      List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A basin.</summary>
public class BasinArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "basin";

    public BasinArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A beach.</summary>
public class BeachArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "beach";

    public BeachArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A bog.</summary>
public class BogArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "bog";

    public BogArea(string displayName, string contextDescription, string transitionDescription,
                   List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A boulder.</summary>
public class BoulderArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "boulder";

    public BoulderArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A chamber.</summary>
public class ChamberArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "chamber";

    public ChamberArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A clearing.</summary>
public class ClearingArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "clearing";

    public ClearingArea(string displayName, string contextDescription, string transitionDescription,
                        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A cliff.</summary>
public class CliffArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "cliff";

    public CliffArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A coop.</summary>
public class CoopArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "coop";

    public CoopArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A courtyard.</summary>
public class CourtyardArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "courtyard";

    public CourtyardArea(string displayName, string contextDescription, string transitionDescription,
                         List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A crag.</summary>
public class CragArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "crag";

    public CragArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A crop.</summary>
public class CropArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "crop";

    public CropArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A crown.</summary>
public class CrownArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "crown";

    public CrownArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A deadwood.</summary>
public class DeadwoodArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "deadwood";

    public DeadwoodArea(string displayName, string contextDescription, string transitionDescription,
                        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A den.</summary>
public class DenArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "den";

    public DenArea(string displayName, string contextDescription, string transitionDescription,
                   List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A ditch.</summary>
public class DitchArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "ditch";

    public DitchArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A entrance.</summary>
public class EntranceArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "entrance";

    public EntranceArea(string displayName, string contextDescription, string transitionDescription,
                        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A estuary.</summary>
public class EstuaryArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "estuary";

    public EstuaryArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A exposed.</summary>
public class ExposedArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "exposed";

    public ExposedArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A forest.</summary>
public class ForestArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "forest";

    public ForestArea(string displayName, string contextDescription, string transitionDescription,
                      List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A forge.</summary>
public class ForgeArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "forge";

    public ForgeArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A garden.</summary>
public class GardenArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "garden";

    public GardenArea(string displayName, string contextDescription, string transitionDescription,
                      List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A gorge.</summary>
public class GorgeArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "gorge";

    public GorgeArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A grassland.</summary>
public class GrasslandArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "grassland";

    public GrasslandArea(string displayName, string contextDescription, string transitionDescription,
                         List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A green.</summary>
public class GreenArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "green";

    public GreenArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A ground.</summary>
public class GroundArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "ground";

    public GroundArea(string displayName, string contextDescription, string transitionDescription,
                      List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A grove.</summary>
public class GroveArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "grove";

    public GroveArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A gully.</summary>
public class GullyArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "gully";

    public GullyArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A hall.</summary>
public class HallArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "hall";

    public HallArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A heath.</summary>
public class HeathArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "heath";

    public HeathArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A hedgerow.</summary>
public class HedgerowArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "hedgerow";

    public HedgerowArea(string displayName, string contextDescription, string transitionDescription,
                        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A herb.</summary>
public class HerbArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "herb";

    public HerbArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A hill.</summary>
public class HillArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "hill";

    public HillArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A ice.</summary>
public class IceArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "ice";

    public IceArea(string displayName, string contextDescription, string transitionDescription,
                   List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A lane.</summary>
public class LaneArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "lane";

    public LaneArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A ledge.</summary>
public class LedgeArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "ledge";

    public LedgeArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A market.</summary>
public class MarketArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "market";

    public MarketArea(string displayName, string contextDescription, string transitionDescription,
                      List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A meadow.</summary>
public class MeadowArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "meadow";

    public MeadowArea(string displayName, string contextDescription, string transitionDescription,
                      List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A memory.</summary>
public class MemoryArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "memory";

    public MemoryArea(string displayName, string contextDescription, string transitionDescription,
                      List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A mill.</summary>
public class MillArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "mill";

    public MillArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A orchard.</summary>
public class OrchardArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "orchard";

    public OrchardArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A pen.</summary>
public class PenArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "pen";

    public PenArea(string displayName, string contextDescription, string transitionDescription,
                   List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A pigsty.</summary>
public class PigstyArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "pigsty";

    public PigstyArea(string displayName, string contextDescription, string transitionDescription,
                      List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A plateau.</summary>
public class PlateauArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "plateau";

    public PlateauArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A pool.</summary>
public class PoolArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "pool";

    public PoolArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A ridge.</summary>
public class RidgeArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "ridge";

    public RidgeArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A rock.</summary>
public class RockArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "rock";

    public RockArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A roof.</summary>
public class RoofArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "roof";

    public RoofArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A room.</summary>
public class RoomArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "room";

    public RoomArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A row.</summary>
public class RowArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "row";

    public RowArea(string displayName, string contextDescription, string transitionDescription,
                   List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A scree.</summary>
public class ScreeArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "scree";

    public ScreeArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A seam.</summary>
public class SeamArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "seam";

    public SeamArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A shaft.</summary>
public class ShaftArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "shaft";

    public ShaftArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A shed.</summary>
public class ShedArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "shed";

    public ShedArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A shore.</summary>
public class ShoreArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "shore";

    public ShoreArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A slope.</summary>
public class SlopeArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "slope";

    public SlopeArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A square.</summary>
public class SquareArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "square";

    public SquareArea(string displayName, string contextDescription, string transitionDescription,
                      List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A stream.</summary>
public class StreamArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "stream";

    public StreamArea(string displayName, string contextDescription, string transitionDescription,
                      List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A summit.</summary>
public class SummitArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "summit";

    public SummitArea(string displayName, string contextDescription, string transitionDescription,
                      List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A thicket.</summary>
public class ThicketArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "thicket";

    public ThicketArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A tree.</summary>
public class TreeArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "tree";

    public TreeArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A tunnel.</summary>
public class TunnelArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "tunnel";

    public TunnelArea(string displayName, string contextDescription, string transitionDescription,
                      List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A valley.</summary>
public class ValleyArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "valley";

    public ValleyArea(string displayName, string contextDescription, string transitionDescription,
                      List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A vegetable.</summary>
public class VegetableArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "vegetable";

    public VegetableArea(string displayName, string contextDescription, string transitionDescription,
                         List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A walk.</summary>
public class WalkArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "walk";

    public WalkArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A waterside.</summary>
public class WatersideArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "waterside";

    public WatersideArea(string displayName, string contextDescription, string transitionDescription,
                         List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A workshop.</summary>
public class WorkshopArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "workshop";

    public WorkshopArea(string displayName, string contextDescription, string transitionDescription,
                        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A yard.</summary>
public class YardArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "yard";

    public YardArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A bedroom.</summary>
public class BedroomArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "bedroom";

    public BedroomArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A dormitory.</summary>
public class DormitoryArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "dormitory";

    public DormitoryArea(string displayName, string contextDescription, string transitionDescription,
                         List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A kitchen.</summary>
public class KitchenArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "kitchen";

    public KitchenArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A pantry.</summary>
public class PantryArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "pantry";

    public PantryArea(string displayName, string contextDescription, string transitionDescription,
                      List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A store.</summary>
public class StoreArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "store";

    public StoreArea(string displayName, string contextDescription, string transitionDescription,
                     List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A landing.</summary>
public class LandingArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "landing";

    public LandingArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A loft.</summary>
public class LoftArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "loft";

    public LoftArea(string displayName, string contextDescription, string transitionDescription,
                    List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A canopy.</summary>
public class CanopyArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "canopy";

    public CanopyArea(string displayName, string contextDescription, string transitionDescription,
                      List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A headland.</summary>
public class HeadlandArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "headland";

    public HeadlandArea(string displayName, string contextDescription, string transitionDescription,
                        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A gallery.</summary>
public class GalleryArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "gallery";

    public GalleryArea(string displayName, string contextDescription, string transitionDescription,
                       List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}
