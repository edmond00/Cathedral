using System.Collections.Generic;

namespace Cathedral.Game.Scene.GetUp;

/// <summary>
/// The protagonist, as an element of the <see cref="RestingPlace"/>: the one thing the two opening
/// phases are played through. It is on the map from the start, observed by nobody — the way a body
/// that has just fallen is — and it <b>holds</b> what each phase is about, as a chest holds what is
/// in it: the fragments of the memory at hand while the childhood comes back
/// (<see cref="Reminescence.FragmentPointOfInterest"/>), the facets of the body's exhaustion when it
/// is time to rise (<see cref="GetUpPointOfInterest"/>). Each is looked at from here and leaves its
/// branch here, so by the last memory the tree under "myself" is the whole childhood.
///
/// <para>No verb acts on it.</para>
/// </summary>
public sealed class SelfPointOfInterest : PointOfInterest
{
    /// <summary>What is said of it on the map, where an observation's text would be.</summary>
    public string Note { get; }

    public SelfPointOfInterest(string note)
        : base(
            displayName:    "Myself",
            referenceLemma: "self",
            descriptions:   new List<string> { "myself, sitting spent at the foot of the tree" },
            items:          null,
            moods:          null)
    {
        Note = note;
    }

    /// <summary>Whether <paramref name="element"/> is something "myself" holds rather than something standing around it.</summary>
    public static bool Holds(Element? element)
        => element is Reminescence.FragmentPointOfInterest or GetUpPointOfInterest;
}
