using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Stray Dog NPC — feral beast, hostile, unpredictable.</summary>
public class StrayDogArchetype : NamedNpcArchetype
{
    public override string ArchetypeId => "stray_dog";
    public override Species Species => SpeciesRegistry.Dog;
    public override bool DefaultEnemy => true;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 5;

    public override string[] NamePool => new[]
    {
        "Gaunt Dog", "Feral Dog", "Snarling Dog", "Scarred Dog",
        "Mangy Hound", "Lean Cur", "Wild Hound", "Half-Starved Dog"
    };

    public override string RoleNoun => "dog";
    protected override bool LabelMentionsLocation => false;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a gaunt dog stands with hackles raised, a low snarl in its throat",
        "a rangy dog circles at a distance, ribs showing through matted fur",
        "a mangy dog bares its teeth, ears flat against its skull",
    };
}
