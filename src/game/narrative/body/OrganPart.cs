namespace Cathedral.Game.Narrative;

/// <summary>
/// Base class for organ parts. An organ part is the smallest scoring unit.
/// For single-part organs, the organ part has the same id as the organ.
/// Each organ part can define a custom MaxScore per class, and this can be
/// overridden further at runtime for specific species (e.g. wolf fangs max 8).
/// Player distributes points into organ parts to build their character.
/// </summary>
public abstract class OrganPart
{
    public abstract string Id { get; }
    public abstract string DisplayName { get; }

    /// <summary>
    /// Class-level default maximum score. Override in subclasses to customize.
    /// </summary>
    public virtual int DefaultMaxScore => 5;

    private int? _speciesMaxScore;

    /// <summary>
    /// Effective maximum score, taking any species-level override into account.
    /// </summary>
    public int MaxScore => _speciesMaxScore ?? DefaultMaxScore;

    /// <summary>
    /// Apply a species-specific cap to this organ part.
    /// Called by <see cref="PartyMember"/> after body parts are instantiated.
    /// </summary>
    internal void SetSpeciesMaxScore(int max) => _speciesMaxScore = max;

    private int _score = 1;

    /// <summary>
    /// Current score (player-allocated). Clamped between 0 and MaxScore.
    /// </summary>
    public int Score
    {
        get => _score;
        set => _score = Math.Clamp(value, 0, MaxScore);
    }
}
