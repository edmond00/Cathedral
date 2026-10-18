namespace Cathedral.Game.Scene;

/// <summary>
/// Points of interest that are parts of one thing — a building's entry door and its outside wall — and
/// are therefore presented to observation as that thing before they are presented apart (see
/// <c>ObservationGrouping</c>). Set by the factory that builds the parts, which is the only place
/// that knows they belong together: by type, never by matching names.
///
/// <para>A connector part sits in two areas' lists. The cluster only forms where at least two of its
/// parts are in view together, so a building's door seen from its own hall stands alone.</para>
/// </summary>
public sealed class PoiCluster
{
    /// <summary>Title of the whole ("Moss-Shouldered Townhouse").</summary>
    public string Name { get; }

    /// <summary>The whole's core noun, the keyword anchor ("house").</summary>
    public string Lemma { get; }

    /// <summary>Noun phrase describing the whole, without article.</summary>
    public string Description { get; }

    public PoiCluster(string name, string lemma, string description)
    {
        Name        = name;
        Lemma       = lemma;
        Description = description;
    }
}
