namespace Cathedral.Game.Narrative;

/// <summary>
/// Base class for all body humors.
/// Body humors have values (0-100 range) representing their current state.
/// </summary>
public abstract class BodyHumor
{
    public string Name { get; protected set; }
    public int Value { get; set; }
    
    protected BodyHumor(string name, int value = 50)
    {
        Name = name;
        Value = value;
    }
}
