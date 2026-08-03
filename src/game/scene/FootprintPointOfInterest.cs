using System.Collections.Generic;

namespace Cathedral.Game.Scene;

/// <summary>
/// Sign left by an animal that passes through here at some hour of the day, but is not here now.
///
/// <para>Placed automatically for every beast in a location, in each area its schedule takes it to.
/// <c>SceneNpcPlacement</c> hides the sign in whichever area the beast currently occupies, because
/// tracking something you are looking at is absurd — and because the sign is only interesting as an
/// answer to "where is it now".</para>
///
/// <para>This is what makes a beast's schedule legible. A wolf that moves between four areas through
/// the day is, without sign, simply absent from three of them; with sign, the wood has a wolf in it
/// and you can work out where.</para>
/// </summary>
public class FootprintPointOfInterest : PointOfInterest
{
    /// <summary>The creature that left this. What <c>TrackVerb</c> follows to wherever it is now.</summary>
    public SceneNpc Quarry { get; }

    public FootprintPointOfInterest(SceneNpc quarry, string displayName, List<string> descriptions)
        : base(displayName, "track", descriptions,
               items: null,
               moods: new[] { "fresh", "half-washed", "clear", "pressed deep" },
               isNatural: true)
    {
        Quarry = quarry;

        // Sign rewards every sense that reads it: what made it, and how recently it passed.
        Senses = new SensoryProfile(Examine: true, Smell: true);
        VerbModiMentis = new Dictionary<string, string>
        {
            ["examine"] = "spoor_reading",
            ["smell"]   = "scenting",
        };
    }
}
