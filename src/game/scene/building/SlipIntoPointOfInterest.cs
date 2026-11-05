using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene.Building;

/// <summary>What sort of gap a <see cref="SlipIntoPointOfInterest"/> is.</summary>
public enum SlipKind
{
    /// <summary>A chimney, entered from the roof. Sooty, tight and one-way in practice.</summary>
    Chimney,

    /// <summary>An unshuttered or broken window. The classic way past a locked door.</summary>
    Window,

    /// <summary>A hatch or trap, usually for goods rather than people.</summary>
    Hatch,

    /// <summary>A gap where the wall or thatch has failed. Nobody meant it to be a way in.</summary>
    Breach,

    /// <summary>A cleft in living rock, a body's width, opening on somewhere enclosed. Wild ground: lawful.</summary>
    Crevice,

    /// <summary>A cave passage low enough to be crawled on the belly, often wet. Wild ground: lawful.</summary>
    Crawlway,

    /// <summary>The split in a hollow tree, a dune's undercut, a gap in a root-mass. Wild ground: lawful.</summary>
    Hollow,
}

/// <summary>
/// A gap too small to be a door, leading somewhere you have not been let into: a chimney from a
/// roof, a broken window, a delivery hatch, a hole in the thatch — or, in the wild, a cleft in the rock,
/// a crawlway, a hollow trunk. Targeted by <see cref="Verbs.SlipIntoVerb"/>, which is illegal where it
/// reaches somebody's private ground and lawful anywhere else.
///
/// <para>This is the payoff for <see cref="ScalePointOfInterest"/>: climbing a house wall gets you a
/// roof, and a roof gets you a chimney, and a chimney gets you inside a locked house without ever
/// touching the door. Gate connector, so no <c>AreaGraph</c> edge beside it.</para>
/// </summary>
public class SlipIntoPointOfInterest : ConnectorPointOfInterest
{
    protected override string ConnectorKind => "slip";

    /// <summary>The side you enter from.</summary>
    public Area OutsideArea => AreaA;

    /// <summary>The place the gap leads into.</summary>
    public Area InsideArea => AreaB;

    /// <summary>What sort of gap this is.</summary>
    public SlipKind Kind { get; }

    /// <summary>
    /// Whether the gap can be used in both directions. A window can be climbed back out of; a chimney
    /// cannot be climbed back up, which is worth knowing before you go down one.
    /// </summary>
    public bool TwoWay => Kind != SlipKind.Chimney;

    public SlipIntoPointOfInterest(
        Area outsideArea,
        Area insideArea,
        SlipKind kind,
        string displayName,
        List<string> descriptions,
        string[]? moods = null)
        : base(outsideArea, insideArea, displayName, LemmaFor(kind), descriptions, moods: moods)
    {
        Kind = kind;
    }

    /// <summary>
    /// A gap the land made rather than a gap in somebody's building. What a will tells apart: a
    /// housebreaker wants a way into a house, a caver a way into the rock.
    /// </summary>
    public bool IsNaturalGap => Kind is SlipKind.Crevice or SlipKind.Crawlway or SlipKind.Hollow;

    /// <summary>How hard the squeeze is.</summary>
    public int Difficulty => Kind switch
    {
        SlipKind.Window  => 4,
        SlipKind.Hatch   => 4,
        SlipKind.Breach  => 5,
        SlipKind.Chimney => 6,
        SlipKind.Crevice => 4,
        SlipKind.Crawlway => 5,
        SlipKind.Hollow  => 3,
        _                => 5,
    };

    /// <summary>
    /// What a failure costs — mostly getting stuck, scraped and an hour working loose, which is the
    /// honest failure mode of forcing a body through a gap meant for smoke.
    /// </summary>
    public IReadOnlyList<FailurePenalty> FailurePenalties() => Kind switch
    {
        SlipKind.Chimney => new FailurePenalty[]
        {
            FailurePenalty.WasteTime, new ScrapeWound(),
            new ContusionWound(), new CutWound(), new ShoulderDislocationLeftWound(),
        },
        // Wedged in the dark with a mountain on top: the hour goes, and the nerve with it.
        SlipKind.Crawlway or SlipKind.Crevice => new FailurePenalty[]
        {
            FailurePenalty.WasteTime, FailurePenalty.WasteTime, FailurePenalty.LoseFocus, new ScrapeWound(),
            new ContusionWound(), new ShoulderDislocationLeftWound(),
        },
        _ => new FailurePenalty[] { FailurePenalty.WasteTime, FailurePenalty.WasteTime, new ScrapeWound(), new CutWound(), new ContusionWound() },
    };

    private static string LemmaFor(SlipKind kind) => kind switch
    {
        SlipKind.Chimney => "chimney",
        SlipKind.Window  => "window",
        SlipKind.Hatch   => "hatch",
        SlipKind.Breach  => "breach",
        SlipKind.Crevice => "cleft",
        SlipKind.Crawlway => "crawlway",
        SlipKind.Hollow  => "bole",
        _                => "gap",
    };
}
