namespace Cathedral.Game.Narrative;

/// <summary>
/// Base class for organ parts. An organ part is the smallest scoring unit.
/// For single-part organs, the organ part has the same id as the organ.
/// Each organ part can define a custom MaxScore.
/// Player distributes points into organ parts to build their character.
/// </summary>
public abstract class OrganPart
{
    public abstract string Id { get; }
    public abstract string DisplayName { get; }
    
    /// <summary>
    /// Maximum score this organ part can have. Override to customize per class.
    /// </summary>
    public virtual int MaxScore => 5;
    
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
