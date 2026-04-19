using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Bear NPC — hostile beast, high HP, hard fight.</summary>
public class BearArchetype : NamedNpcArchetype
{
    public override string ArchetypeId => "bear";
    public override Species Species => SpeciesRegistry.Bear;
    public override bool DefaultHostile => true;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 6;

    public override string[] NamePool => new[]
    {
        "Brown Bear", "Cave Bear", "Grizzled Bear", "Great Bear",
        "Scarred Bear", "Old Bear", "Young Bear", "Massive Bear"
    };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a {name.ToLowerInvariant()} stands upright nearby, sniffing the air with a low growl";
}
