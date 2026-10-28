using System.Collections.Generic;

namespace Cathedral.Game.Scene;

// ─────────────────────────────────────────────────────────────────────────────
//  The area kinds the wilderness overhaul added. Same contract as AreaKinds.cs: the kind is what a
//  place is, the display name what this one is called, and the lemma is derived from the class name
//  and fixed here. Several are shared between biomes (a col on a mountain, a peak and a snowfield; a
//  river in a cave, the hot steppe and the jungle), which is the point of a kind: content asks what
//  sort of place it is, not which factory built it.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>A waterfall and the pool it falls into.</summary>
public class WaterfallArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "waterfall";

    public WaterfallArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A high summer pasture with its stone hut, left empty until the herds come up.</summary>
public class ShielingArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "shieling";

    public ShielingArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A saddle between two heights, where the wind comes through.</summary>
public class ColArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "col";

    public ColArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A steep gully of old snow and fallen stone running down a mountainside.</summary>
public class CouloirArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "couloir";

    public CouloirArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A ledge high on a crag where a great bird nests.</summary>
public class EyrieArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "eyrie";

    public EyrieArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A space inside the rock reached through a crack a body's width.</summary>
public class CleftArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "cleft";

    public CleftArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A top that looked like the summit from below and is not.</summary>
public class FalseSummitArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "falsesummit";

    public FalseSummitArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A sheltered nook below the top where a climber can sit out the night.</summary>
public class BivouacArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "bivouac";

    public BivouacArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A shelter under a jutting roof of rock, open to the day on one side.</summary>
public class OverhangArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "overhang";

    public OverhangArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A hole where the ground has fallen into a cave below, open to the sky.</summary>
public class SinkholeArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "sinkhole";

    public SinkholeArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A chamber where bats hang in thousands and the floor is deep in their droppings.</summary>
public class ColonyArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "colony";

    public ColonyArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A hall whose walls and floor are dripstone: curtains, columns, rimmed pools.</summary>
public class FlowstoneArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "flowstone";

    public FlowstoneArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A river: running water too deep to wade, with ground along its banks.</summary>
public class RiverArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "river";

    public RiverArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A drop in the dark, its bottom reached only by a climb.</summary>
public class ChasmArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "chasm";

    public ChasmArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>The floor of a natural shaft where animals fell and were never got out.</summary>
public class PitArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "pit";

    public PitArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A bank of rounded stones thrown up by the sea.</summary>
public class ShingleArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "shingle";

    public ShingleArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>Grazing marsh cut by tidal creeks, flooded at the high springs.</summary>
public class SaltmarshArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "saltmarsh";

    public SaltmarshArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A flat of grey mud the tide leaves bare, alive with worm and bird.</summary>
public class MudflatArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "mudflat";

    public MudflatArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A cave the sea has cut into the foot of a cliff.</summary>
public class SeaCaveArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "seacave";

    public SeaCaveArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A rock standing off the shore, reached by swimming the channel.</summary>
public class IsletArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "islet";

    public IsletArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A cliff ledge where seabirds nest packed wing to wing.</summary>
public class RookeryArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "rookery";

    public RookeryArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>Wet woodland of alder and willow standing in black water.</summary>
public class CarrArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "carr";

    public CarrArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A pond: still water, a bank, whatever lives at the edge.</summary>
public class PondArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "pond";

    public PondArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A steep wooded cleft cut by a stream, its floor a climb below the rest.</summary>
public class RavineArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "ravine";

    public RavineArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>The place a stream rises out of the ground.</summary>
public class WellspringArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "wellspring";

    public WellspringArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>Woodland cut back to the stool and grown again in rods.</summary>
public class CoppiceArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "coppice";

    public CoppiceArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A sunken lane worn deep by centuries of feet, roofed over by the banks.</summary>
public class HollowayArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "holloway";

    public HollowayArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>Meadow by a river, flooded in winter on purpose and lush in spring.</summary>
public class WaterMeadowArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "watermeadow";

    public WaterMeadowArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A shallow place where a river is crossed on foot.</summary>
public class FordArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "ford";

    public FordArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A loop of old river cut off from the stream, still and reedy.</summary>
public class OxbowArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "oxbow";

    public OxbowArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A long grassy mound raised over the dead of a forgotten people.</summary>
public class BarrowArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "barrow";

    public BarrowArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A ring of standing stones on open ground.</summary>
public class HengeArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "henge";

    public HengeArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>Flat wet ground of sedge and reed fed by the land's own water.</summary>
public class FenArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "fen";

    public FenArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>Rough open ground of gorse and bracken where anyone may graze.</summary>
public class CommonArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "common";

    public CommonArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A lone hill of bare rock rising straight out of the flat.</summary>
public class InselbergArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "inselberg";

    public InselbergArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A field of rock ridges carved by the wind into long hulls.</summary>
public class YardangArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "yardang";

    public YardangArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A churned mud hole where the big animals come to roll.</summary>
public class WallowArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "wallow";

    public WallowArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A patch of salty earth that animals come from miles to lick.</summary>
public class LickArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "lick";

    public LickArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A strip of tall green trees along a river, in country that is otherwise dry.</summary>
public class RiverwoodArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "riverwood";

    public RiverwoodArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>Ground thick with termite mounds taller than a man.</summary>
public class TermitaryArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "termitary";

    public TermitaryArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A bar of sand in a river, uncovered when the water falls.</summary>
public class SandbarArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "sandbar";

    public SandbarArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A round well in the rock open to the sky, with clear water at its bottom.</summary>
public class CenoteArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "cenote";

    public CenoteArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A bowl at the head of a side canyon where the walls close all round.</summary>
public class AmphitheatreArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "amphitheatre";

    public AmphitheatreArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>Bare clay hills gullied by the rain into ribs and spires.</summary>
public class BadlandsArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "badlands";

    public BadlandsArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A field of broken ice fragments churned together on the water.</summary>
public class BrashArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "brash";

    public BrashArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>The fast ice frozen to the shore, rough and tide-cracked.</summary>
public class IcefootArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "icefoot";

    public IcefootArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>Ice where seals haul out to lie by their breathing holes.</summary>
public class HauloutArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "haulout";

    public HauloutArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A lake: open water wide enough that its far shore is a long swim.</summary>
public class LakeArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "lake";

    public LakeArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>The high field of old snow the glacier is fed from.</summary>
public class NeveArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "neve";

    public NeveArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>The end of the glacier, where the ice breaks off and the meltwater runs out.</summary>
public class SnoutArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "snout";

    public SnoutArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>A plain of gravel and braided streams washed out from under the ice.</summary>
public class OutwashArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "outwash";

    public OutwashArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>The jumble of snow and broken trees where an avalanche came to rest.</summary>
public class RunoutArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "runout";

    public RunoutArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>Wet sphagnum ground over frozen soil, that quakes and swallows a step.</summary>
public class MuskegArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "muskeg";

    public MuskegArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}

/// <summary>Ground the frost has sorted into rings and stripes of stone.</summary>
public class FrostfieldArea : Area
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "frostfield";

    public FrostfieldArea(string displayName, string contextDescription, string transitionDescription,
        List<string> descriptions, string[]? moods = null, bool isPrivate = false)
        : base(displayName, Lemma, contextDescription, transitionDescription, descriptions, moods, isPrivate) { }
}
