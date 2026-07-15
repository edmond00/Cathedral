using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Wolf NPC — hostile beast, pack hunter, moderate difficulty.</summary>
public class WolfArchetype : NamedNpcArchetype
{
    public override string ArchetypeId => "wolf";
    public override Species Species => SpeciesRegistry.Wolf;
    public override bool DefaultEnemy => true;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 6;

    // A wolf met in the wild is grown but rarely old: 2–9 years.
    public override int MinAgeDays => 2 * LifetimeStat.DaysPerYear;
    public override int MaxAgeDays => 9 * LifetimeStat.DaysPerYear;

    public override string[] NamePool => new[]
    {
        "Grey Wolf", "Timber Wolf", "Black Wolf", "Lean Wolf",
        "Scarred Wolf", "Young Wolf", "Old Wolf", "Lone Wolf"
    };

    public override string RoleNoun => "wolf";
    protected override bool LabelMentionsLocation => false;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a lean grey shape watches from the shadows, yellow eyes gleaming",
        "a grey wolf paces at the tree-line, head low and eyes fixed",
        "a rangy wolf bares its fangs, a growl rolling from its chest",
    };
}
