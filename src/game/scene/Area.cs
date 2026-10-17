using System.Collections.Generic;

namespace Cathedral.Game.Scene;

/// <summary>
/// A specific space within a <see cref="Section"/>.
/// Contains the <see cref="PointOfInterest"/>s the player can focus on.
/// Connected to other areas via directed edges in <see cref="Scene.AreaGraph"/>.
/// </summary>
public class Area : Element
{
    public override string DisplayName { get; }
    public override List<string> Descriptions { get; }

    /// <summary>
    /// Core noun used as the keyword-similarity anchor when this area becomes an observation.
    /// Defined explicitly at every construction site (no inference) — e.g. "grassland".
    /// </summary>
    public string ReferenceLemma { get; }

    /// <summary>Context description for LLM prompts (e.g. "crossing the open grassland").</summary>
    public string ContextDescription { get; }

    /// <summary>Transition description for LLM prompts (e.g. "move into the grassland").</summary>
    public string TransitionDescription { get; }

    /// <summary>Points of interest (large objects, features) directly within this area.</summary>
    public List<PointOfInterest> PointsOfInterest { get; } = new();

    /// <summary>Mood adjectives for procedural neutral descriptions.</summary>
    public string[] Moods { get; }

    /// <summary>
    /// Whose ground this is: somebody's (<see cref="AreaTenure.Private"/>), everybody's
    /// (<see cref="AreaTenure.Public"/>) or nobody's (<see cref="AreaTenure.Wild"/>). Factories set
    /// private and public; wild is stamped afterwards by <see cref="SceneFactory"/> on the open ground
    /// of a wilderness location. <see cref="PrivacyModel"/> is what reads it.
    /// </summary>
    public AreaTenure Tenure { get; set; }

    /// <summary>
    /// When true, entering or taking items from this area without permission is an illegal action.
    /// Setting it false makes the area public, never wild — the wilderness pass runs after every
    /// factory has had its say.
    /// </summary>
    public bool IsPrivate
    {
        get => Tenure == AreaTenure.Private;
        set => Tenure = value ? AreaTenure.Private : AreaTenure.Public;
    }

    /// <summary>Nobody's ground: see <see cref="AreaTenure.Wild"/>.</summary>
    public bool IsWild => Tenure == AreaTenure.Wild;


    public Area(
        string displayName,
        string referenceLemma,
        string contextDescription,
        string transitionDescription,
        List<string> descriptions,
        string[]? moods = null,
        bool isPrivate = false)
    {
        DisplayName           = displayName;
        ReferenceLemma        = referenceLemma;
        ContextDescription    = contextDescription;
        TransitionDescription = transitionDescription;
        Descriptions          = descriptions;
        Moods      = moods ?? System.Array.Empty<string>();
        IsPrivate  = isPrivate;
    }
}

/// <summary>Whose ground an <see cref="Area"/> is — the question <see cref="PrivacyModel"/> answers from.</summary>
public enum AreaTenure
{
    /// <summary>Everybody's: a street, a square, a farm's field. Lawful to stand in; its beasts are somebody's.</summary>
    Public,

    /// <summary>Somebody's: a house, a bedroom, a back room. Anything done here is trespass.</summary>
    Private,

    /// <summary>
    /// Nobody's: the open ground of a forest, a plain, a cave, a coast, a mountain or a climate biome.
    /// Lawful to stand in, and a beast that lives here belongs to no one, so striking or killing it is
    /// hunting, not a crime. Crimes against people are crimes here as anywhere.
    /// </summary>
    Wild,
}
