using System.Collections.Generic;

namespace Cathedral.Game.Scene.GetUp;

/// <summary>
/// The protagonist, as an element of the <see cref="RestingPlace"/>: the one thing the two opening
/// phases are played through. It <b>holds</b> what each phase is about, as a chest holds what is
/// in it: the fragments of the memory at hand while the childhood comes back
/// (<see cref="Reminescence.FragmentPointOfInterest"/>), the facets of the body's exhaustion when it
/// is time to rise (<see cref="GetUpPointOfInterest"/>). What it holds is shown unnamed until it
/// is looked at: a memory is not known before it comes back.
///
/// <para>While the childhood comes back it is there from the start and is the whole of the
/// screen. Getting up, it is one more thing under the tree, to be noticed like the rest — a
/// person, the only one there — and what it holds is disclosed by looking at it.</para>
///
/// <para>No verb acts on it.</para>
/// </summary>
public sealed class SelfPointOfInterest : PointOfInterest
{
    /// <summary>What is said of it on the map, where an observation's text would be.</summary>
    public string Note { get; }

    /// <summary>What one of the things it holds is called before it is looked at: "a memory", "a sensation".</summary>
    public string Holding { get; }

    public SelfPointOfInterest(string note, string holding)
        : base(
            displayName:    "Myself",
            referenceLemma: "self",
            descriptions:   new List<string> { "myself, sitting spent at the foot of the tree" },
            items:          null,
            moods:          null)
    {
        Note = note;
        Holding = holding;
    }

    /// <summary>Whether <paramref name="element"/> is something "myself" holds rather than something standing around it.</summary>
    public static bool Holds(Element? element)
        => element is Reminescence.FragmentPointOfInterest or GetUpPointOfInterest;
}
