using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Black Bear NPC — powerful beast, hostile, high HP.</summary>
public class BlackBearArchetype : NamedNpcArchetype
{
    public override string ArchetypeId => "black_bear";
    public override Species Species => SpeciesRegistry.Bear;
    public override bool DefaultEnemy => true;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 7;

    public override string[] NamePool => new[]
    {
        "Black Bear", "Shaggy Bear", "Boar-Scarred Bear", "Lean Bear",
        "Old Black Bear", "Young Bear", "Heavy Bear", "Scarred Bear"
    };

    public override string RoleNoun => "black bear";
    protected override bool LabelMentionsLocation => false;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a black bear lifts its broad head and sniffs the air, a low grunt rolling in its chest",
        "a glossy black bear turns from a torn log, muzzle wet",
        "a dark bear rears and huffs, small eyes fixing on you",
    };
}
