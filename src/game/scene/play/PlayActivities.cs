using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.ModiMentis;

namespace Cathedral.Game.Scene;

// ─────────────────────────────────────────────────────────────────────────────
//  Every kind of play in the game. Each says, for itself, what is done (the verbatim), how hard it is,
//  what the body needs, what it teaches and what a miss costs — PlayVerb reads all five from here.
//
//  Lessons: most of them are taught nowhere else. Gambling, plunge, surefoot/balance, whistling and
//  howling are the existing minds a game also practises. Order beast before human wherever a beast
//  may play: a beast's lesson names an organ a person lacks, so a person falls through to theirs.
//
//  Wounds: only the wildcard grazes (contusion, cut, scrape, strain) where a beast may play, since a
//  named wound a beast's body does not own is set aside and the draw made again.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Flat stones sent skipping across still water: ducks and drakes.</summary>
public sealed class SkippingStonesPlay : PlayActivity
{
    private readonly string _how;

    /// <param name="how">The act, with <c>{0}</c> for the water — or, on a beach of pebbles, for the stones.</param>
    public SkippingStonesPlay(string how = "skip flat stones across {0}, counting the bounces") => _how = how;

    public override string Verbatim(string thing) => string.Format(_how, thing);
    public override int Difficulty => 3;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;
    public override IEnumerable<ModusMentis> Lessons { get { yield return Mm<DucksAndDrakesModusMentis>(); } }

    /// <summary>A stone that will not skip is a wrenched shoulder from throwing it harder.</summary>
    public override IReadOnlyList<FailurePenalty> FailurePenalties => new FailurePenalty[]
    {
        FailurePenalty.LoseFocus, FailurePenalty.LoseFocus, new StrainWound(),
    };
}

/// <summary>Out over the drop on a hanging liana or a rope tied to a branch, and back.</summary>
public sealed class SwingPlay : PlayActivity
{
    public override string Verbatim(string thing) => $"take hold of {thing}, run and swing out as far as the arc will carry me";
    public override int Difficulty => 4;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;
    public override IEnumerable<ModusMentis> Lessons { get { yield return Mm<SwingingModusMentis>(); } }

    /// <summary>The hands let go too soon, or the trunk comes round too fast.</summary>
    public override IReadOnlyList<FailurePenalty> FailurePenalties => new FailurePenalty[]
    {
        FailurePenalty.LoseFocus, new ContusionWound(), new StrainWound(), new ScrapeWound(),
    };
}

/// <summary>
/// Stroking, scratching and fussing over a beast that will allow it. The one play declared on a
/// creature rather than a thing, and the one whose wording differs by creature: a cat is stroked, a
/// dog is scratched behind the ears, a lamb is held.
/// </summary>
public sealed class PettingPlay : PlayActivity
{
    private readonly string _verbatim;
    private readonly int _difficulty;
    private readonly bool _claws;

    /// <param name="verbatim">The act, with <c>{0}</c> for the creature's pronoun.</param>
    /// <param name="difficulty">How wary the creature is.</param>
    /// <param name="claws">Whether a fumbled approach is met with claws or teeth rather than a sidestep.</param>
    public PettingPlay(string verbatim, int difficulty, bool claws)
    {
        _verbatim   = verbatim;
        _difficulty = difficulty;
        _claws      = claws;
    }

    public override string Verbatim(string thing) => string.Format(_verbatim, thing);
    public override int Difficulty => _difficulty;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;
    public override IEnumerable<ModusMentis> Lessons { get { yield return Mm<CossetingModusMentis>(); } }

    public override IReadOnlyList<FailurePenalty> FailurePenalties => _claws
        ? new FailurePenalty[] { FailurePenalty.LoseFocus, new CutWound() }
        : new FailurePenalty[] { FailurePenalty.LoseFocus, FailurePenalty.LoseFocus, FailurePenalty.WasteTime };
}

/// <summary>Running the hens round the yard for the noise they make — a game a dog plays as well as a child.</summary>
public sealed class ChasePlay : PlayActivity
{
    private readonly string _verbatim;
    public ChasePlay(string verbatim) => _verbatim = verbatim;

    public override string Verbatim(string thing) => string.Format(_verbatim, thing);
    public override int Difficulty => 2;
    public override IEnumerable<ModusMentis> Lessons { get { yield return Mm<ChasingModusMentis>(); } }

    public override IReadOnlyList<FailurePenalty> FailurePenalties => new FailurePenalty[]
    {
        FailurePenalty.WasteTime, FailurePenalty.LoseFocus, new StrainWound(), new ScrapeWound(),
    };
}

/// <summary>A run-up and a long slide on a strip of polished ice.</summary>
public sealed class IceSlidePlay : PlayActivity
{
    public override string Verbatim(string thing) => $"take a run at {thing} and slide its whole length standing";
    public override int Difficulty => 3;
    public override IEnumerable<ModusMentis> Lessons { get { yield return Mm<GlissadeModusMentis>(); } }

    public override IReadOnlyList<FailurePenalty> FailurePenalties => new FailurePenalty[]
    {
        FailurePenalty.LoseFocus, new ContusionWound(), new ContusionWound(), new StrainWound(),
    };
}

/// <summary>Down a slope of snow or sand on the seat, or down a stair the same way: the slide.</summary>
public sealed class SlideDownPlay : PlayActivity
{
    private readonly bool _needsHands;
    private readonly string _how;

    /// <param name="how">The act, with <c>{0}</c> for the slope.</param>
    /// <param name="needsHands">A rail or a stair wants hands to hold on; a snowbank does not.</param>
    public SlideDownPlay(string how, bool needsHands = false)
    {
        _how        = how;
        _needsHands = needsHands;
    }

    public override string Verbatim(string thing) => string.Format(_how, thing);
    public override int Difficulty => 2;
    public override AnatomyCapability RequiredCapabilities
        => _needsHands ? AnatomyCapability.Handcraft : AnatomyCapability.None;
    public override IEnumerable<ModusMentis> Lessons { get { yield return Mm<GlissadeModusMentis>(); } }

    public override IReadOnlyList<FailurePenalty> FailurePenalties => new FailurePenalty[]
    {
        FailurePenalty.LoseFocus, FailurePenalty.LoseFocus, new ContusionWound(), new ScrapeWound(),
    };
}

/// <summary>A castle of wet sand, with a moat for the tide to fill.</summary>
public sealed class SandcastlePlay : PlayActivity
{
    public override string Verbatim(string thing) => $"build a castle out of {thing}, with towers and a moat";
    public override int Difficulty => 2;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;
    public override IEnumerable<ModusMentis> Lessons { get { yield return Mm<SandCastlesModusMentis>(); } }

    /// <summary>It slumps, and the morning goes with it.</summary>
    public override IReadOnlyList<FailurePenalty> FailurePenalties => new FailurePenalty[]
    {
        FailurePenalty.LoseFocus, FailurePenalty.WasteTime,
    };
}

/// <summary>Both feet into a puddle of mud, for the splash.</summary>
public sealed class MudPuddlePlay : PlayActivity
{
    public override string Verbatim(string thing) => $"jump into {thing} with both feet and stamp until the mud flies";
    public override int Difficulty => 1;
    public override IEnumerable<ModusMentis> Lessons { get { yield return Mm<MudlarkingModusMentis>(); } }

    /// <summary>Sat down in it, and an hour gone scraping it off.</summary>
    public override IReadOnlyList<FailurePenalty> FailurePenalties => new FailurePenalty[]
    {
        FailurePenalty.WasteTime, FailurePenalty.LoseFocus, new StrainWound(),
    };
}

/// <summary>Kicking through a drift of dead leaves and falling into it: play with nothing to learn but play.</summary>
public sealed class LeafDriftPlay : PlayActivity
{
    public override string Verbatim(string thing) => $"kick through {thing} and throw myself down into it";
    public override int Difficulty => 1;
    public override IEnumerable<ModusMentis> Lessons { get { yield return Mm<PlayfulnessModusMentis>(); } }
}

/// <summary>Knucklebones thrown and caught on the back of the hand — the game soldiers and drinkers play for stakes.</summary>
public sealed class KnucklebonesPlay : PlayActivity
{
    public override string Verbatim(string thing) => $"play a round with {thing}, tossing them up and catching them on the back of my hand";
    public override int Difficulty => 3;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft | AnatomyCapability.Abstraction;
    public override IEnumerable<ModusMentis> Lessons { get { yield return Mm<GamblingModusMentis>(); } }

    public override IReadOnlyList<FailurePenalty> FailurePenalties => new FailurePenalty[]
    {
        FailurePenalty.LoseFocus, FailurePenalty.LoseFocus, FailurePenalty.WasteTime,
    };
}

/// <summary>Hopping the chalked squares of a hopscotch: a game with rules, which is why a beast is not offered it.</summary>
public sealed class HopscotchPlay : PlayActivity
{
    public override string Verbatim(string thing) => $"hop {thing} on one foot, out to the last square and back";
    public override int Difficulty => 2;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;
    public override IEnumerable<ModusMentis> Lessons { get { yield return Mm<HopscotchModusMentis>(); } }

    public override IReadOnlyList<FailurePenalty> FailurePenalties => new FailurePenalty[]
    {
        FailurePenalty.LoseFocus, FailurePenalty.LoseFocus, new StrainWound(),
    };
}

/// <summary>A shout thrown at rock to hear it come back. A beast howls at it and learns howling.</summary>
public sealed class EchoPlay : PlayActivity
{
    public override string Verbatim(string thing) => $"call out at {thing} and listen for the echo to answer";
    public override int Difficulty => 1;
    public override IEnumerable<ModusMentis> Lessons
    {
        get
        {
            yield return Mm<HowlingModusMentis>();
            yield return Mm<HallooModusMentis>();
        }
    }
}

/// <summary>Lying down at the top of a bank, or a heap of hay, and rolling to the bottom.</summary>
public sealed class TumblePlay : PlayActivity
{
    private readonly string _how;

    /// <param name="how">The act, with <c>{0}</c> for the bank or the heap.</param>
    public TumblePlay(string how) => _how = how;

    public override string Verbatim(string thing) => string.Format(_how, thing);
    public override int Difficulty => 1;
    public override IEnumerable<ModusMentis> Lessons { get { yield return Mm<TumblingModusMentis>(); } }

    public override IReadOnlyList<FailurePenalty> FailurePenalties => new FailurePenalty[]
    {
        FailurePenalty.LoseFocus, new ContusionWound(), new ScrapeWound(),
    };
}

/// <summary>A leap off a rock into deep cold water, for the shock of it.</summary>
public sealed class PoolJumpPlay : PlayActivity
{
    private readonly string _how;

    /// <param name="how">The act, with <c>{0}</c> for the rock or the falls.</param>
    public PoolJumpPlay(string how = "climb {0} and leap off it into the deep water below") => _how = how;

    public override string Verbatim(string thing) => string.Format(_how, thing);
    public override int Difficulty => 4;
    public override IEnumerable<ModusMentis> Lessons { get { yield return Mm<PlungeModusMentis>(); } }

    /// <summary>A bad entry is a slap that knocks the wind out, or the shallow ledge found by a knee.</summary>
    public override IReadOnlyList<FailurePenalty> FailurePenalties => new FailurePenalty[]
    {
        FailurePenalty.WasteTime, new ContusionWound(), new ContusionWound(), new StrainWound(),
    };
}

/// <summary>Stone to stone across running water and back again without a wet foot.</summary>
public sealed class SteppingStonesPlay : PlayActivity
{
    public override string Verbatim(string thing) => $"hop along {thing} from one to the next and back without a wet foot";
    public override int Difficulty => 3;
    public override IEnumerable<ModusMentis> Lessons
    {
        get
        {
            yield return Mm<SurefootModusMentis>();
            yield return Mm<BalanceModusMentis>();
        }
    }

    public override IReadOnlyList<FailurePenalty> FailurePenalties => new FailurePenalty[]
    {
        FailurePenalty.WasteTime, new StrainWound(), new ContusionWound(),
    };
}

/// <summary>A broad blade of grass stretched between the thumbs and blown till it shrieks.</summary>
public sealed class GrassWhistlePlay : PlayActivity
{
    public override string Verbatim(string thing) => $"pull a blade from {thing}, stretch it between my thumbs and blow until it shrieks";
    public override int Difficulty => 2;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;
    public override IEnumerable<ModusMentis> Lessons { get { yield return Mm<WhistlingModusMentis>(); } }
}

/// <summary>Wading and splashing in shallow water for the cold and the noise.</summary>
public sealed class PaddlePlay : PlayActivity
{
    public override string Verbatim(string thing) => $"wade into {thing} and kick the water up in sprays";
    public override int Difficulty => 1;
    public override IEnumerable<ModusMentis> Lessons { get { yield return Mm<PaddlingModusMentis>(); } }

    public override IReadOnlyList<FailurePenalty> FailurePenalties => new FailurePenalty[]
    {
        FailurePenalty.WasteTime, FailurePenalty.LoseFocus, new ScrapeWound(),
    };
}

/// <summary>Something thrown at a mark for sport — snowballs, clods at a scarecrow, pebbles at a post.</summary>
public sealed class ThrowAtMarkPlay : PlayActivity
{
    private readonly string _how;

    /// <param name="how">The act, with <c>{0}</c> for the mark or the snow.</param>
    public ThrowAtMarkPlay(string how) => _how = how;

    public override string Verbatim(string thing) => string.Format(_how, thing);
    public override int Difficulty => 2;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;
    public override IEnumerable<ModusMentis> Lessons { get { yield return Mm<ShyingModusMentis>(); } }

    public override IReadOnlyList<FailurePenalty> FailurePenalties => new FailurePenalty[]
    {
        FailurePenalty.LoseFocus, FailurePenalty.LoseFocus, new StrainWound(),
    };
}

/// <summary>The length of a fallen trunk walked with the arms out.</summary>
public sealed class BalanceWalkPlay : PlayActivity
{
    public override string Verbatim(string thing) => $"climb up on {thing} and walk its whole length with my arms out";
    public override int Difficulty => 3;
    public override IEnumerable<ModusMentis> Lessons
    {
        get
        {
            yield return Mm<SurefootModusMentis>();
            yield return Mm<BalanceModusMentis>();
        }
    }

    public override IReadOnlyList<FailurePenalty> FailurePenalties => new FailurePenalty[]
    {
        FailurePenalty.LoseFocus, new StrainWound(), new ScrapeWound(),
    };
}

/// <summary>A child's toy left lying — a top to whip, a cup and ball — and a while spent at it.</summary>
public sealed class ToyPlay : PlayActivity
{
    public override string Verbatim(string thing) => $"pick up {thing} and keep it going as long as I can";
    public override int Difficulty => 2;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;
    public override IEnumerable<ModusMentis> Lessons { get { yield return Mm<WhirligigModusMentis>(); } }

    public override IReadOnlyList<FailurePenalty> FailurePenalties => new FailurePenalty[]
    {
        FailurePenalty.LoseFocus, FailurePenalty.LoseFocus, FailurePenalty.WasteTime,
    };
}
