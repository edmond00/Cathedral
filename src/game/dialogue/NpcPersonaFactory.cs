namespace Cathedral.Game.Dialogue;

/// <summary>
/// Abstract factory that produces an NpcPersona for a specific NPC archetype.
/// </summary>
public abstract class NpcPersonaFactory
{
    public abstract NpcPersona CreatePersona();
}
