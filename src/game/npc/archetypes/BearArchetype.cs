using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Bear NPC — hostile beast, high HP, hard fight.</summary>
public class BearArchetype : NamedNpcArchetype
{
    public override string ArchetypeId => "bear";
    public override Species Species => SpeciesRegistry.Bear;
    public override bool DefaultEnemy => true;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 6;

    public override string[] NamePool => new[]
    {
        "Brown Bear", "Cave Bear", "Grizzled Bear", "Great Bear",
        "Scarred Bear", "Old Bear", "Young Bear", "Massive Bear"
    };

    public override string RoleNoun => "bear";
    protected override bool LabelMentionsLocation => false;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a great shaggy bear stands upright nearby, sniffing the air with a low growl",
        "a heavy brown bear swings its head toward you, claws raking the dirt",
        "a hulking bear rises onto its hind legs, a rumble building in its chest",
    };
}
