using System.Collections.Generic;

namespace Cathedral.Game.Scene;

/// <summary>
/// Somewhere to sit: a bench, a stump, a low wall, a mounting block, the tail of a cart.
///
/// <para>The point of it is <see cref="Verbs.SitAndWaitVerb"/> — the only way in the game to move the
/// clock on deliberately. Until this existed the time of day was drawn once when you arrived at a
/// location and could not be changed, so everything gated on a period (a shop's opening hours, who is
/// in the square, whether a door is shut for the night) was decided for you by a die roll before you
/// had done anything.</para>
///
/// <para>Sit spots are also the plainest example of the multi-verb rule: something you can sit on is
/// nearly always also something to look at and somewhere to listen from, so one small object carries
/// three or four actions.</para>
/// </summary>
public class SitSpotPointOfInterest : PointOfInterest
{
    public SitSpotPointOfInterest(
        string displayName,
        string referenceLemma,
        List<string> descriptions,
        string[]? moods = null,
        bool isNatural = false,
        IReadOnlyDictionary<string, string>? verbModiMentis = null)
        : base(displayName, referenceLemma, descriptions, items: null, moods: moods, isNatural: isNatural)
    {
        if (verbModiMentis != null) VerbModiMentis = verbModiMentis;
    }
}

/// <summary>
/// Somewhere to get out of sight and stay there: a hay bale, a dense bush, a hollow under roots, a
/// wood pile, an upturned cart.
///
/// <para>Targeted by <see cref="Verbs.HideAndWaitVerb"/>, which passes time until the population of
/// the location changes — somebody arrives, or somebody leaves. That is a different kind of waiting
/// from a sit spot: sitting spends exactly one period, hiding spends as many as it takes.</para>
/// </summary>
public class HidingPointOfInterest : PointOfInterest
{
    public HidingPointOfInterest(
        string displayName,
        string referenceLemma,
        List<string> descriptions,
        string[]? moods = null,
        bool isNatural = false,
        IReadOnlyDictionary<string, string>? verbModiMentis = null)
        : base(displayName, referenceLemma, descriptions, items: null, moods: moods, isNatural: isNatural)
    {
        if (verbModiMentis != null) VerbModiMentis = verbModiMentis;
    }
}
