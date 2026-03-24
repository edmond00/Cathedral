using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Bear NPC — hostile beast, high HP, hard fight.</summary>
public class BearArchetype : NpcArchetype
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

    protected override string[] BuildNarrationKeywords(string name)
        => new[] { "bear", "massive", "claws", "growl", "fur", "beast", "lumber", "den" };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a {name.ToLowerInvariant()} stands upright nearby, sniffing the air with a low growl";
}
