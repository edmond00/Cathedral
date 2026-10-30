using System.Collections.Generic;

namespace Cathedral.Game.Scene;

// ─────────────────────────────────────────────────────────────────────────────
//  The kinds of thing that exist to be played at — a rope swing, a mud puddle, a chalked hopscotch.
//  Each carries its play from construction, so wherever one is built PLAY is offered on it; most are
//  laid by PlayPlacement over a finished scene, and a factory may build one like any other kind.
//
//  The lemma is derived from the class name, as in PoiKinds.cs. The display name and the description
//  are per site; the defaults are what PlayPlacement uses when it names nothing better.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Flat pebbles washed smooth at the water's edge, thin enough to skip. Played at: <see cref="SkippingStonesPlay"/>.</summary>
public class PebblesPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "pebbles";

    /// <summary>What it looks like when the site says nothing more particular.</summary>
    public const string DefaultDescription = "Flat pebbles washed smooth at the water's edge, thin enough to skip";

    public PebblesPointOfInterest(string displayName, List<string>? descriptions = null, string[]? moods = null)
        : base(displayName, Lemma, descriptions ?? new() { DefaultDescription }, items: null, moods: moods, isNatural: true)
    {
        Play   = new SkippingStonesPlay("pick the flattest of {0} and skip them across the water, counting the bounces");
        Senses = SensoryProfile.Examinable;
    }
}

/// <summary>A rope swing knotted to a high branch, its loop worn smooth by hands. Played at: <see cref="SwingPlay"/>.</summary>
public class SwingPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "swing";

    /// <summary>What it looks like when the site says nothing more particular.</summary>
    public const string DefaultDescription = "A rope swing knotted to a high branch, its loop worn smooth by hands";

    public SwingPointOfInterest(string displayName, List<string>? descriptions = null, string[]? moods = null)
        : base(displayName, Lemma, descriptions ?? new() { DefaultDescription }, items: null, moods: moods, isNatural: false)
    {
        Play   = new SwingPlay();
        Senses = SensoryProfile.Examinable;
    }
}

/// <summary>A strip of ice polished glassy by feet that have run and slid along it. Played at: <see cref="IceSlidePlay"/>.</summary>
public class SlidePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "slide";

    /// <summary>What it looks like when the site says nothing more particular.</summary>
    public const string DefaultDescription = "A strip of ice polished glassy by feet that have run and slid along it";

    public SlidePointOfInterest(string displayName, List<string>? descriptions = null, string[]? moods = null)
        : base(displayName, Lemma, descriptions ?? new() { DefaultDescription }, items: null, moods: moods, isNatural: true)
    {
        Play   = new IceSlidePlay();
        Senses = SensoryProfile.Examinable;
    }
}

/// <summary>A long bank of soft snow sloping down to a level run-out. Played at: <see cref="SlideDownPlay"/>.</summary>
public class SnowbankPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "snowbank";

    /// <summary>What it looks like when the site says nothing more particular.</summary>
    public const string DefaultDescription = "A long bank of soft snow sloping down to a level run-out";

    public SnowbankPointOfInterest(string displayName, List<string>? descriptions = null, string[]? moods = null)
        : base(displayName, Lemma, descriptions ?? new() { DefaultDescription }, items: null, moods: moods, isNatural: true)
    {
        Play   = new SlideDownPlay("sit at the top of {0} and slide down it on my seat");
        Senses = SensoryProfile.Examinable;
    }
}

/// <summary>The steep lee face of a dune, sand ready to pour at a touch. Played at: <see cref="SlideDownPlay"/>.</summary>
public class SlipfacePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "slipface";

    /// <summary>What it looks like when the site says nothing more particular.</summary>
    public const string DefaultDescription = "The steep lee face of a dune, sand ready to pour at a touch";

    public SlipfacePointOfInterest(string displayName, List<string>? descriptions = null, string[]? moods = null)
        : base(displayName, Lemma, descriptions ?? new() { DefaultDescription }, items: null, moods: moods, isNatural: true)
    {
        Play   = new SlideDownPlay("throw myself down {0} and ride the pouring sand to the bottom");
        Senses = SensoryProfile.Examinable;
    }
}

/// <summary>A stretch of firm wet sand left smooth by the last tide. Played at: <see cref="SandcastlePlay"/>.</summary>
public class SandPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "sand";

    /// <summary>What it looks like when the site says nothing more particular.</summary>
    public const string DefaultDescription = "A stretch of firm wet sand left smooth by the last tide";

    public SandPointOfInterest(string displayName, List<string>? descriptions = null, string[]? moods = null)
        : base(displayName, Lemma, descriptions ?? new() { DefaultDescription }, items: null, moods: moods, isNatural: true)
    {
        Play   = new SandcastlePlay();
        Senses = SensoryProfile.Examinable;
    }
}

/// <summary>A broad brown puddle of mud, churned at the edges. Played at: <see cref="MudPuddlePlay"/>.</summary>
public class PuddlePointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "puddle";

    /// <summary>What it looks like when the site says nothing more particular.</summary>
    public const string DefaultDescription = "A broad brown puddle of mud, churned at the edges";

    public PuddlePointOfInterest(string displayName, List<string>? descriptions = null, string[]? moods = null)
        : base(displayName, Lemma, descriptions ?? new() { DefaultDescription }, items: null, moods: moods, isNatural: true)
    {
        Play   = new MudPuddlePlay();
        Senses = SensoryProfile.Odorous;
    }
}

/// <summary>Dead leaves drifted knee-deep in a hollow, dry and rustling. Played at: <see cref="LeafDriftPlay"/>.</summary>
public class LeavesPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "leaves";

    /// <summary>What it looks like when the site says nothing more particular.</summary>
    public const string DefaultDescription = "Dead leaves drifted knee-deep in a hollow, dry and rustling";

    public LeavesPointOfInterest(string displayName, List<string>? descriptions = null, string[]? moods = null)
        : base(displayName, Lemma, descriptions ?? new() { DefaultDescription }, items: null, moods: moods, isNatural: true)
    {
        Play   = new LeafDriftPlay();
        Senses = SensoryProfile.Audible;
    }
}

/// <summary>A handful of sheep's knucklebones left on a board, worn smooth and yellow. Played at: <see cref="KnucklebonesPlay"/>.</summary>
public class KnucklebonesPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "knucklebones";

    /// <summary>What it looks like when the site says nothing more particular.</summary>
    public const string DefaultDescription = "A handful of sheep's knucklebones left on a board, worn smooth and yellow";

    public KnucklebonesPointOfInterest(string displayName, List<string>? descriptions = null, string[]? moods = null)
        : base(displayName, Lemma, descriptions ?? new() { DefaultDescription }, items: null, moods: moods, isNatural: false)
    {
        Play   = new KnucklebonesPlay();
        Senses = SensoryProfile.Examinable;
    }
}

/// <summary>Squares chalked on the ground and numbered, a flat stone lying in the first. Played at: <see cref="HopscotchPlay"/>.</summary>
public class HopscotchPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "hopscotch";

    /// <summary>What it looks like when the site says nothing more particular.</summary>
    public const string DefaultDescription = "Squares chalked on the ground and numbered, a flat stone lying in the first";

    public HopscotchPointOfInterest(string displayName, List<string>? descriptions = null, string[]? moods = null)
        : base(displayName, Lemma, descriptions ?? new() { DefaultDescription }, items: null, moods: moods, isNatural: false)
    {
        Play   = new HopscotchPlay();
        Senses = SensoryProfile.Examinable;
    }
}

/// <summary>A face of bare stone that throws every sound back. Played at: <see cref="EchoPlay"/>.</summary>
public class EchoPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "echo";

    /// <summary>What it looks like when the site says nothing more particular.</summary>
    public const string DefaultDescription = "A face of bare stone that throws every sound back";

    public EchoPointOfInterest(string displayName, List<string>? descriptions = null, string[]? moods = null)
        : base(displayName, Lemma, descriptions ?? new() { DefaultDescription }, items: null, moods: moods, isNatural: true)
    {
        Play   = new EchoPlay();
        Senses = SensoryProfile.Audible;
    }
}

/// <summary>A steep grassy bank, smooth and soft to the bottom. Played at: <see cref="TumblePlay"/>.</summary>
public class BankPointOfInterest : PointOfInterest, ISeat
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "bank";

    /// <summary>What it looks like when the site says nothing more particular.</summary>
    public const string DefaultDescription = "A steep grassy bank, smooth and soft to the bottom";

    public BankPointOfInterest(string displayName, List<string>? descriptions = null, string[]? moods = null)
        : base(displayName, Lemma, descriptions ?? new() { DefaultDescription }, items: null, moods: moods, isNatural: true)
    {
        Play   = new TumblePlay("lie down at the top of {0} and roll over and over to the bottom");
        Senses = SensoryProfile.Beautiful;
    }
}

/// <summary>A shelf of rock standing high over deep, dark, cold water. Played at: <see cref="PoolJumpPlay"/>.</summary>
public class LeapPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "leap";

    /// <summary>What it looks like when the site says nothing more particular.</summary>
    public const string DefaultDescription = "A shelf of rock standing high over deep, dark, cold water";

    public LeapPointOfInterest(string displayName, List<string>? descriptions = null, string[]? moods = null)
        : base(displayName, Lemma, descriptions ?? new() { DefaultDescription }, items: null, moods: moods, isNatural: true)
    {
        Play   = new PoolJumpPlay();
        Senses = SensoryProfile.Examinable;
    }
}

/// <summary>A line of flat stones set across the water a stride apart. Played at: <see cref="SteppingStonesPlay"/>.</summary>
public class StonesPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "stones";

    /// <summary>What it looks like when the site says nothing more particular.</summary>
    public const string DefaultDescription = "A line of flat stones set across the water a stride apart";

    public StonesPointOfInterest(string displayName, List<string>? descriptions = null, string[]? moods = null)
        : base(displayName, Lemma, descriptions ?? new() { DefaultDescription }, items: null, moods: moods, isNatural: true)
    {
        Play   = new SteppingStonesPlay();
        Senses = SensoryProfile.Audible;
    }
}

/// <summary>Long grass grown up thick, its blades broad and flat. Played at: <see cref="GrassWhistlePlay"/>.</summary>
public class GrassPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "grass";

    /// <summary>What it looks like when the site says nothing more particular.</summary>
    public const string DefaultDescription = "Long grass grown up thick, its blades broad and flat";

    public GrassPointOfInterest(string displayName, List<string>? descriptions = null, string[]? moods = null)
        : base(displayName, Lemma, descriptions ?? new() { DefaultDescription }, items: null, moods: moods, isNatural: true)
    {
        Play   = new GrassWhistlePlay();
        Senses = SensoryProfile.Fragrant;
    }
}

/// <summary>Clear shallow water over pale stones, cold to the ankle. Played at: <see cref="PaddlePlay"/>.</summary>
public class ShallowsPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "shallows";

    /// <summary>What it looks like when the site says nothing more particular.</summary>
    public const string DefaultDescription = "Clear shallow water over pale stones, cold to the ankle";

    public ShallowsPointOfInterest(string displayName, List<string>? descriptions = null, string[]? moods = null)
        : base(displayName, Lemma, descriptions ?? new() { DefaultDescription }, items: null, moods: moods, isNatural: true)
    {
        Play   = new PaddlePlay();
        Senses = SensoryProfile.Audible;
    }
}

/// <summary>Fresh snow lying deep and clean, wet enough to pack. Played at: <see cref="ThrowAtMarkPlay"/>.</summary>
public class SnowPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "snow";

    /// <summary>What it looks like when the site says nothing more particular.</summary>
    public const string DefaultDescription = "Fresh snow lying deep and clean, wet enough to pack";

    public SnowPointOfInterest(string displayName, List<string>? descriptions = null, string[]? moods = null)
        : base(displayName, Lemma, descriptions ?? new() { DefaultDescription }, items: null, moods: moods, isNatural: true)
    {
        Play   = new ThrowAtMarkPlay("pack {0} into balls and throw them at a mark until one hits it square");
        Senses = SensoryProfile.Examinable;
    }
}

/// <summary>A child's plaything left lying, worn with handling. Played at: <see cref="ToyPlay"/>.</summary>
public class ToyPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "toy";

    /// <summary>What it looks like when the site says nothing more particular.</summary>
    public const string DefaultDescription = "A child's plaything left lying, worn with handling";

    public ToyPointOfInterest(string displayName, List<string>? descriptions = null, string[]? moods = null)
        : base(displayName, Lemma, descriptions ?? new() { DefaultDescription }, items: null, moods: moods, isNatural: false)
    {
        Play   = new ToyPlay();
        Senses = SensoryProfile.Examinable;
    }
}

/// <summary>A post set up as a mark, chipped and dented where things have hit it. Played at: <see cref="ThrowAtMarkPlay"/>.</summary>
public class MarkPointOfInterest : PointOfInterest
{
    /// <summary>The lemma this kind answers to. Derived from the class name and fixed here.</summary>
    public const string Lemma = "mark";

    /// <summary>What it looks like when the site says nothing more particular.</summary>
    public const string DefaultDescription = "A post set up as a mark, chipped and dented where things have hit it";

    public MarkPointOfInterest(string displayName, List<string>? descriptions = null, string[]? moods = null)
        : base(displayName, Lemma, descriptions ?? new() { DefaultDescription }, items: null, moods: moods, isNatural: false)
    {
        Play   = new ThrowAtMarkPlay("gather a fistful of pebbles and pelt {0} until I hit it square");
        Senses = SensoryProfile.Examinable;
    }
}
