namespace Cathedral.Game.Dialogue.Demo;

/// <summary>
/// Factory that produces the InnKeeper persona.
/// </summary>
public class InnKeeperPersonaFactory : NpcPersonaFactory
{
    public override NpcPersona CreatePersona() => new InnKeeperPersona();
}
