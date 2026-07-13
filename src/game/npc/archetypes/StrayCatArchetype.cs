using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Stray Cat NPC — cautious beast, non-hostile, territorial.</summary>
public class StrayCatArchetype : NamedNpcArchetype
{
    public override string ArchetypeId => "stray_cat";
    public override Species Species => SpeciesRegistry.Cat;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 4;

    public override string[] NamePool => new[]
    {
        "Tabby Cat", "Grey Cat", "Black Cat", "Tortoiseshell Cat",
        "Thin Cat", "Old Cat", "Wild Cat", "Scarred Cat"
    };

    public override string RoleNoun => "cat";
    protected override bool LabelMentionsLocation => false;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a lean cat regards you with narrowed eyes, tail twitching",
        "a ragged-eared cat crouches on a wall, watching every move",
        "a wary cat slinks along the shadow of a fence, low to the ground",
    };
}
