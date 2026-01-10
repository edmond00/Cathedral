namespace Cathedral.Game.Narrative;

/// <summary>
/// Base class for all body parts.
/// Body parts have levels (1-10) used for skill checks.
/// </summary>
public abstract class BodyPart
{
    public string Name { get; protected set; }
    public string Description { get; protected set; }
    public int Level { get; set; }
    
    protected BodyPart(string name, string description, int level = 1)
    {
        Name = name;
        Description = description;
        Level = level;
    }
}
