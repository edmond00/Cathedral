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
}

/// <summary>
/// A gap too small to be a door, leading somewhere you have not been let into: a chimney from a
/// roof, a broken window, a delivery hatch, a hole in the thatch. Targeted by
/// <see cref="Verbs.SlipIntoVerb"/>, which is illegal — that is the point of it.
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

    /// <summary>How hard the squeeze is.</summary>
    public int Difficulty => Kind switch
    {
        SlipKind.Window  => 4,
        SlipKind.Hatch   => 4,
        SlipKind.Breach  => 5,
        SlipKind.Chimney => 6,
        _                => 5,
    };

    /// <summary>
    /// What a failure costs — mostly getting stuck and scraped, which is the honest failure mode of
    /// forcing a body through a gap meant for smoke.
    /// </summary>
    public IReadOnlyList<Wound?> FailurePenalties() => Kind switch
    {
        SlipKind.Chimney => new Wound?[]
        {
            null, null,
            new ContusionWound(), new CutWound(), new ShoulderDislocationLeftWound(),
        },
        _ => new Wound?[] { null, null, null, new CutWound(), new ContusionWound() },
    };

    private static string LemmaFor(SlipKind kind) => kind switch
    {
        SlipKind.Chimney => "chimney",
        SlipKind.Window  => "window",
        SlipKind.Hatch   => "hatch",
        SlipKind.Breach  => "breach",
        _                => "gap",
    };
}
