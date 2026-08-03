using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene.Building;

/// <summary>What is being climbed, which decides how hard it is and what a fall costs.</summary>
public enum ScaleKind
{
    /// <summary>The outside wall of a building, up to its roof.</summary>
    Wall,

    /// <summary>A tree big enough to climb into. Plenty to hold; a long way down.</summary>
    Tree,

    /// <summary>The inside of a well shaft, up or down. Wet, narrow, no room to fall clear.</summary>
    WellShaft,

    /// <summary>A ladder or timber stage left in place. The easy end of climbing.</summary>
    Ladder,

    /// <summary>A stack of something — hay, timber, barrels — climbed for the height it gives.</summary>
    Stack,
}

/// <summary>
/// Something climbable that reaches a place with no other way in: a house wall up to its roof, a
/// great tree up into its canopy, a well shaft down to the water.
///
/// <para>Deliberately distinct from <see cref="CliffPointOfInterest"/>, which is natural rock in
/// open country. A scale point is nearly always man-made or man-adjacent, its top area is nearly
/// always somewhere you are not supposed to be, and the tops are where the horizon can be
/// observed — the reason for climbing being, most of the time, to see rather than to arrive.</para>
///
/// <para>Gate connector: no <c>AreaGraph</c> edge beside it, or the roof is simply a room.</para>
/// </summary>
public class ScalePointOfInterest : ConnectorPointOfInterest
{
    protected override string ConnectorKind => "scale";

    /// <summary>The area at the foot.</summary>
    public Area BottomArea => AreaA;

    /// <summary>The area at the top, reachable only by climbing.</summary>
    public Area TopArea => AreaB;

    /// <summary>What sort of thing this is.</summary>
    public ScaleKind Kind { get; }

    public ScalePointOfInterest(
        Area bottomArea,
        Area topArea,
        ScaleKind kind,
        string displayName,
        List<string> descriptions,
        string[]? moods = null)
        : base(bottomArea, topArea, displayName, LemmaFor(kind), descriptions, moods: moods,
               isNatural: kind == ScaleKind.Tree)
    {
        Kind = kind;
    }

    /// <summary>How hard the climb is. A ladder is nearly free; a bare wall is not.</summary>
    public int Difficulty => Kind switch
    {
        ScaleKind.Ladder    => 2,
        ScaleKind.Stack     => 3,
        ScaleKind.Tree      => 4,
        ScaleKind.WellShaft => 5,
        ScaleKind.Wall      => 5,
        _                   => 4,
    };

    /// <summary>
    /// What a fall costs. Height is the whole of it: falling off a hay stack is a winding, falling
    /// off a roof is not.
    /// </summary>
    public IReadOnlyList<Wound?> FailurePenalties() => Kind switch
    {
        ScaleKind.Ladder => new Wound?[] { null, null, null, null, new ContusionWound() },
        ScaleKind.Stack  => new Wound?[] { null, null, null, null, new ContusionWound(), new WristFractureRightWound() },
        ScaleKind.WellShaft => new Wound?[]
        {
            null, null,
            new ContusionWound(), new BrokenRibsWound(), new AnkleFractureLeftWound(), new ConcussionsWound(),
        },
        _ => new Wound?[]
        {
            null, null, null,
            new AnkleFractureRightWound(), new TibiaFractureLeftWound(),
            new BrokenArmLeftWound(), new WristFractureLeftWound(),
        },
    };

    private static string LemmaFor(ScaleKind kind) => kind switch
    {
        ScaleKind.Wall      => "wall",
        ScaleKind.Tree      => "tree",
        ScaleKind.WellShaft => "well",
        ScaleKind.Ladder    => "ladder",
        ScaleKind.Stack     => "stack",
        _                   => "climb",
    };
}
