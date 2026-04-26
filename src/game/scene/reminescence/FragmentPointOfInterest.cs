using System.Collections.Generic;
using Cathedral.Game.Narrative.Reminescence;

namespace Cathedral.Game.Scene.Reminescence;

/// <summary>
/// A point of interest representing a single childhood-reminescence fragment.
/// Only the <see cref="FragmentData.ObservationText"/> is surfaced here — intentionally vague
/// and impressionistic. The concrete memory is revealed after REMEMBER fires, via the outcome
/// narration block, which uses <see cref="FragmentData.OutcomeText"/>.
/// </summary>
public sealed class FragmentPointOfInterest : PointOfInterest
{
    /// <summary>The static fragment data (observation text, outcome text, outcome).</summary>
    public FragmentData Fragment { get; }

    public FragmentPointOfInterest(FragmentData fragment)
        : base(
            displayName:  fragment.Name,
            descriptions: new List<string> { fragment.ObservationText },
            items:        null,
            moods:        new[] { "fuzzy", "fading", "half-remembered", "hazy", "shimmering", "elusive", "distant" })
    {
        Fragment = fragment;
    }
}
