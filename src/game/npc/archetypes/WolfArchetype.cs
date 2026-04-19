using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Wolf NPC — hostile beast, pack hunter, moderate difficulty.</summary>
public class WolfArchetype : NamedNpcArchetype
{
    public override string ArchetypeId => "wolf";
    public override Species Species => SpeciesRegistry.Wolf;
    public override bool DefaultHostile => true;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 6;

    public override string[] NamePool => new[]
    {
        "Grey Wolf", "Timber Wolf", "Black Wolf", "Lean Wolf",
        "Scarred Wolf", "Young Wolf", "Old Wolf", "Lone Wolf"
    };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a {name.ToLowerInvariant()} watches from the shadows, yellow eyes gleaming";
}
