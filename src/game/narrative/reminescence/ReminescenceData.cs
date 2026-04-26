using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Reminescence;

/// <summary>
/// Static description of a single childhood reminescence: the ambient memory the
/// protagonist is currently dwelling in, plus the set of fragments they can REMEMBER.
/// </summary>
public sealed class ReminescenceData
{
    /// <summary>Stable identifier ("sound_in_the_dark", "farm_childhood", ...).</summary>
    public string Id { get; }

    /// <summary>
    /// Bullet-style paragraphs describing the reminescence. The first line is used as
    /// the area's neutral description; the rest are folded into the location context.
    /// </summary>
    public IReadOnlyList<string> ContentLines { get; }

    /// <summary>The fragments the player can REMEMBER from this reminescence.</summary>
    public IReadOnlyList<FragmentData> Fragments { get; }

    public ReminescenceData(string id, IReadOnlyList<string> contentLines, IReadOnlyList<FragmentData> fragments)
    {
        Id           = id;
        ContentLines = contentLines;
        Fragments    = fragments;
    }
}
