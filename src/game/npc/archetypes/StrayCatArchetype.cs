using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Stray Cat NPC — cautious beast, non-hostile, territorial.</summary>
public class StrayCatArchetype : NamedNpcArchetype
{
    public override string ArchetypeId => "stray_cat";
    public override Species Species => SpeciesRegistry.Cat;
    public override bool DefaultHostile => false;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 4;

    public override string[] NamePool => new[]
    {
        "Tabby Cat", "Grey Cat", "Black Cat", "Tortoiseshell Cat",
        "Thin Cat", "Old Cat", "Wild Cat", "Scarred Cat"
    };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a {name.ToLowerInvariant()} regards you with narrowed eyes, tail twitching";
}
