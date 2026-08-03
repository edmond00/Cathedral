using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene.Building;

/// <summary>
/// What sort of obstacle a <see cref="CrossingPointOfInterest"/> is. The kind decides the name, the
/// prose, how hard it is, and what a failed attempt costs — brambles tear, scree turns an ankle,
/// mud only soaks you.
/// </summary>
public enum CrossingKind
{
    /// <summary>A dense thicket of thorns. Tears rather than breaks.</summary>
    Brambles,

    /// <summary>Churned ground that swallows a boot. Easy, humiliating, occasionally a wrenched knee.</summary>
    MudPuddle,

    /// <summary>A fallen trunk used as a bridge over something. Balance, then a drop.</summary>
    FallenTrunk,

    /// <summary>Loose broken stone on a slope. Everything about it wants to slide.</summary>
    Scree,

    /// <summary>A stand of nettles. Barely an obstacle; entirely unpleasant.</summary>
    Nettles,

    /// <summary>A dry-stone wall or thorn hedge laid as a boundary. Made to stop livestock, not people.</summary>
    Hedgerow,
}

/// <summary>
/// A dry obstacle between two areas that has to be got across rather than walked round: a bramble
/// thicket, a mud wallow, a fallen trunk, a scree slope, a boundary hedge.
///
/// <para>Like every gate connector this must <b>not</b> be paired with an <c>AreaGraph</c> edge, or
/// <c>MoveToAreaVerb</c> walks past it for difficulty 1 and the obstacle is decorative. Targeted by
/// <see cref="Verbs.CrossVerb"/>; see <see cref="WaterCrossingPointOfInterest"/> for the wet
/// equivalent, which is swum rather than crossed.</para>
/// </summary>
public class CrossingPointOfInterest : ConnectorPointOfInterest
{
    protected override string ConnectorKind => "crossing";

    /// <summary>What sort of obstacle this is.</summary>
    public CrossingKind Kind { get; }

    public CrossingPointOfInterest(
        Area areaA,
        Area areaB,
        CrossingKind kind,
        string displayName,
        List<string> descriptions,
        string[]? moods = null,
        IReadOnlyDictionary<string, string>? verbModiMentis = null)
        : base(areaA, areaB, displayName, LemmaFor(kind), descriptions, moods: moods, isNatural: true)
    {
        Kind = kind;
        if (verbModiMentis != null) VerbModiMentis = verbModiMentis;
    }

    /// <summary>
    /// How hard this is to get across. Deliberately spread: a mud puddle you will nearly always
    /// manage and a bramble thicket you often will not, so the same verb reads differently depending
    /// on what it is aimed at.
    /// </summary>
    public int Difficulty => Kind switch
    {
        CrossingKind.MudPuddle   => 2,
        CrossingKind.Nettles     => 3,
        CrossingKind.Hedgerow    => 4,
        CrossingKind.FallenTrunk => 4,
        CrossingKind.Scree       => 5,
        CrossingKind.Brambles    => 5,
        _                        => 4,
    };

    /// <summary>
    /// The lesson getting across this teaches. Split by what the obstacle actually asks of you:
    /// a trunk and a wall want balance and a vault, mud and scree want footing.
    /// </summary>
    public string ModusMentisId => Kind switch
    {
        CrossingKind.FallenTrunk => "vaulting",
        CrossingKind.Hedgerow    => "vaulting",
        CrossingKind.Brambles    => "hedgecraft",
        CrossingKind.Nettles     => "hedgecraft",
        _                        => "surefoot",
    };

    /// <summary>
    /// What a failed crossing costs. Weighted with nulls so most failures are simply a failure — you
    /// got halfway and backed out — and the injuries that do land suit the obstacle.
    /// </summary>
    public IReadOnlyList<Wound?> FailurePenalties() => Kind switch
    {
        CrossingKind.Brambles => new Wound?[]
        {
            null, null, null, null,
            new CutWound(), new CutWound(), new ScarWound(),
        },
        CrossingKind.Nettles => new Wound?[] { null, null, null, null, null, new ContusionWound() },
        CrossingKind.MudPuddle => new Wound?[] { null, null, null, null, null, new KneeFractureLeftWound() },
        CrossingKind.FallenTrunk => new Wound?[]
        {
            null, null, null,
            new ContusionWound(), new WristFractureLeftWound(), new AnkleFractureRightWound(),
        },
        CrossingKind.Scree => new Wound?[]
        {
            null, null, null,
            new AnkleFractureLeftWound(), new TibiaFractureRightWound(), new ContusionWound(),
        },
        CrossingKind.Hedgerow => new Wound?[]
        {
            null, null, null, null,
            new CutWound(), new ShoulderDislocationRightWound(),
        },
        _ => new Wound?[] { null, null, null, new ContusionWound() },
    };

    /// <summary>
    /// The keyword-similarity anchor. Deliberately the obstacle's own noun rather than a shared
    /// "crossing", so a player typing "brambles" reaches the brambles.
    /// </summary>
    private static string LemmaFor(CrossingKind kind) => kind switch
    {
        CrossingKind.Brambles    => "bramble",
        CrossingKind.MudPuddle   => "mud",
        CrossingKind.FallenTrunk => "trunk",
        CrossingKind.Scree       => "scree",
        CrossingKind.Nettles     => "nettle",
        CrossingKind.Hedgerow    => "hedge",
        _                        => "crossing",
    };
}
