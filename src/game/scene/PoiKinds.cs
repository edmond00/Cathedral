using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene;

// ─────────────────────────────────────────────────────────────────────────────
//  What a point of interest IS, as a type rather than as a string.
//
//  A kind used to be carried by `referenceLemma: "trough"` — a string written at the construction
//  site and read back by verb gates and lesson conditions, with nothing connecting the two ends. So
//  a condition could name a kind that had never been built and everything reported fine: half the
//  lesson conditions in the game named furniture that did not exist, and the modi mentis behind them
//  were unreachable from the day they were written.
//
//  As types that is a compile error. `ctx.Target is Altar` does not build unless an Altar exists,
//  and reflection over these classes answers the other half — a kind no factory constructs is now
//  findable, exactly as `--outcome-audit` finds an Outcome nothing produces.
//
//  The lemma is DERIVED from the class name, so the two cannot drift and saved routines keep
//  resolving. The display name stays a per-site argument: the kind is what a thing is, the name is
//  what this one is called — a Barrel Stack and a Brew Barrel are both Barrel.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>A anvil.</summary>
public class AnvilPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "anvil";

    public AnvilPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A barrel.</summary>
public class BarrelPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "barrel";

    public BarrelPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A bedroll.</summary>
public class BedrollPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "bedroll";

    public BedrollPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                  string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A bellows.</summary>
public class BellowsPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "bellows";

    public BellowsPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                  string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A bread.</summary>
public class BreadPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "bread";

    public BreadPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A churn.</summary>
public class ChurnPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "churn";

    public ChurnPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A cloth.</summary>
public class ClothPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "cloth";

    public ClothPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A coal.</summary>
public class CoalPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "coal";

    public CoalPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A crevice.</summary>
public class CrevicePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "crevice";

    public CrevicePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                  string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A crop.</summary>
public class CropPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "crop";

    public CropPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A cross.</summary>
public class CrossPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "cross";

    public CrossPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A ditch.</summary>
public class DitchPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "ditch";

    public DitchPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A fire.</summary>
public class FirePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "fire";

    public FirePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A flax.</summary>
public class FlaxPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "flax";

    public FlaxPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A flour.</summary>
public class FlourPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "flour";

    public FlourPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A frame.</summary>
public class FramePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "frame";

    public FramePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A grain.</summary>
public class GrainPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "grain";

    public GrainPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A hay.</summary>
public class HayPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "hay";

    public HayPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                              string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A hive.</summary>
public class HivePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "hive";

    public HivePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A hoop.</summary>
public class HoopPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "hoop";

    public HoopPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A lamb.</summary>
public class LambPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "lamb";

    public LambPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A lantern.</summary>
public class LanternPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "lantern";

    public LanternPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                  string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A loom.</summary>
public class LoomPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "loom";

    public LoomPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A marker.</summary>
public class MarkerPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "marker";

    public MarkerPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A millstone.</summary>
public class MillstonePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "millstone";

    public MillstonePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                    string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A mud.</summary>
public class MudPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "mud";

    public MudPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                              string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A mug.</summary>
public class MugPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "mug";

    public MugPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                              string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A nest.</summary>
public class NestPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "nest";

    public NestPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A net.</summary>
public class NetPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "net";

    public NetPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                              string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A ore.</summary>
public class OrePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "ore";

    public OrePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                              string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A oven.</summary>
public class OvenPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "oven";

    public OvenPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A pail.</summary>
public class PailPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "pail";

    public PailPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A post.</summary>
public class PostPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "post";

    public PostPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A rack.</summary>
public class RackPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "rack";

    public RackPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A roost.</summary>
public class RoostPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "roost";

    public RoostPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A rubble.</summary>
public class RubblePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "rubble";

    public RubblePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A sack.</summary>
public class SackPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "sack";

    public SackPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A scarecrow.</summary>
public class ScarecrowPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "scarecrow";

    public ScarecrowPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                    string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A shaving.</summary>
public class ShavingPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "shaving";

    public ShavingPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                  string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A shelf.</summary>
public class ShelfPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "shelf";

    public ShelfPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A stall.</summary>
public class StallPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "stall";

    public StallPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A stave.</summary>
public class StavePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "stave";

    public StavePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A stocks.</summary>
public class StocksPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "stocks";

    public StocksPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A toll.</summary>
public class TollPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "toll";

    public TollPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A tool.</summary>
public class ToolPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "tool";

    public ToolPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A trough.</summary>
public class TroughPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "trough";

    public TroughPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A well.</summary>
public class WellPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "well";

    public WellPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A willow.</summary>
public class WillowPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "willow";

    public WillowPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A wood.</summary>
public class WoodPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "wood";

    public WoodPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A wool.</summary>
public class WoolPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "wool";

    public WoolPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A workbench.</summary>
public class WorkbenchPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "workbench";

    public WorkbenchPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                    string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A bell.</summary>
public class BellPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "bell";

    public BellPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A block.</summary>
public class BlockPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "block";

    public BlockPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A chest.</summary>
public class ChestPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "chest";

    public ChestPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A firewood.</summary>
public class FirewoodPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "firewood";

    public FirewoodPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                   string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A herb.</summary>
public class HerbPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "herb";

    public HerbPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A midden.</summary>
public class MiddenPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "midden";

    public MiddenPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A pallet.</summary>
public class PalletPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "pallet";

    public PalletPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A peg.</summary>
public class PegPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "peg";

    public PegPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                              string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>
/// Consecrated ground: the churchyard turf a parish is buried in. Derives from
/// <see cref="DiggableGroundPointOfInterest"/> rather than from <see cref="PointOfInterest"/>,
/// because a grave is dug — it must satisfy DIG's gate as well as being its own kind, and a kind
/// that is also a mechanism belongs under the mechanism.
/// </summary>
public class GravePointOfInterest : DiggableGroundPointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public new const string Lemma = "grave";

    public GravePointOfInterest(string displayName, List<string> descriptions,
                                List<ItemElement>? items = null, string[]? moods = null)
        : base(displayName, Lemma, descriptions, items, moods) { }
}

/// <summary>A beetroot.</summary>
public class BeetrootPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "beetroot";

    public BeetrootPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                   string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A bench.</summary>
public class BenchPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "bench";

    public BenchPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A boulder.</summary>
public class BoulderPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "boulder";

    public BoulderPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                  string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A bush.</summary>
public class BushPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "bush";

    public BushPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A cabbage.</summary>
public class CabbagePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "cabbage";

    public CabbagePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                  string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A cairn.</summary>
public class CairnPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "cairn";

    public CairnPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A candle.</summary>
public class CandlePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "candle";

    public CandlePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A carrot.</summary>
public class CarrotPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "carrot";

    public CarrotPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A chamomile.</summary>
public class ChamomilePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "chamomile";

    public ChamomilePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                    string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A cradle.</summary>
public class CradlePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "cradle";

    public CradlePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A deadfall.</summary>
public class DeadfallPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "deadfall";

    public DeadfallPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                   string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A driftwood.</summary>
public class DriftwoodPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "driftwood";

    public DriftwoodPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                    string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A flower.</summary>
public class FlowerPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "flower";

    public FlowerPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A hearth.</summary>
public class HearthPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "hearth";

    public HearthPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A hollow.</summary>
public class HollowPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "hollow";

    public HollowPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A ice.</summary>
public class IcePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "ice";

    public IcePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                              string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A kelp.</summary>
public class KelpPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "kelp";

    public KelpPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A leek.</summary>
public class LeekPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "leek";

    public LeekPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A lichen.</summary>
public class LichenPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "lichen";

    public LichenPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A light.</summary>
public class LightPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "light";

    public LightPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A log.</summary>
public class LogPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "log";

    public LogPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                              string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A mat.</summary>
public class MatPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "mat";

    public MatPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                              string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A mint.</summary>
public class MintPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "mint";

    public MintPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A mortar.</summary>
public class MortarPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "mortar";

    public MortarPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>
/// A moss bank. Derives from <see cref="DiggableGroundPointOfInterest"/> because turf is <i>cut</i>
/// from it — DIG only accepts diggable ground, so as a plain point of interest a moss bank could
/// never be dug and the turfcutting lesson keyed to it could never fire.
/// </summary>
public class MossPointOfInterest : DiggableGroundPointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "moss";

    public MossPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = true)
        : base(displayName, Lemma, descriptions, items, moods) { }
}

/// <summary>A mushroom.</summary>
public class MushroomPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "mushroom";

    public MushroomPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                   string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A onion.</summary>
public class OnionPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "onion";

    public OnionPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A parsnip.</summary>
public class ParsnipPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "parsnip";

    public ParsnipPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                  string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A pea.</summary>
public class PeaPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "pea";

    public PeaPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                              string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A pool.</summary>
public class PoolPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "pool";

    public PoolPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A pot.</summary>
public class PotPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "pot";

    public PotPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                              string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A psalter.</summary>
public class PsalterPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "psalter";

    public PsalterPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                  string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A radish.</summary>
public class RadishPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "radish";

    public RadishPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A reed.</summary>
public class ReedPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "reed";

    public ReedPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A rock.</summary>
public class RockPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "rock";

    public RockPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A sage.</summary>
public class SagePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "sage";

    public SagePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A stool.</summary>
public class StoolPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "stool";

    public StoolPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A stream.</summary>
public class StreamPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "stream";

    public StreamPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A stump.</summary>
public class StumpPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "stump";

    public StumpPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A table.</summary>
public class TablePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "table";

    public TablePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A thyme.</summary>
public class ThymePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "thyme";

    public ThymePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A tree.</summary>
public class TreePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "tree";

    public TreePointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                               string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A turnip.</summary>
public class TurnipPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "turnip";

    public TurnipPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                 string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A undergrowth.</summary>
public class UndergrowthPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "undergrowth";

    public UndergrowthPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                      string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A washstand.</summary>
public class WashstandPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "washstand";

    public WashstandPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                    string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A wheel.</summary>
public class WheelPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "wheel";

    public WheelPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}

/// <summary>A wormwood.</summary>
public class WormwoodPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "wormwood";

    public WormwoodPointOfInterest(string displayName, List<string> descriptions, List<ItemElement>? items = null,
                                   string[]? moods = null, bool isNatural = false)
        : base(displayName, Lemma, descriptions, items, moods, isNatural) { }
}
