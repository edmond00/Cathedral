using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Boar NPC — hostile beast, charges, moderate fight.</summary>
public class BoarArchetype : NamedNpcArchetype
{
    public override string ArchetypeId => "boar";
    public override Species Species => SpeciesRegistry.Boar;
    public override bool DefaultEnemy => true;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 6;

    public override string[] NamePool => new[]
    {
        "Wild Boar", "Tusked Boar", "Great Boar", "Mud Boar",
        "Scarred Boar", "Old Boar", "Bristled Boar", "Feral Boar"
    };

    public override string RoleNoun => "boar";
    protected override bool LabelMentionsLocation => false;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a bristling boar roots aggressively in the undergrowth, tusks gleaming",
        "a heavy boar wheels to face you, hackles stiff along its spine",
        "a tusked boar snorts and paws the earth, ready to charge",
    };
}
