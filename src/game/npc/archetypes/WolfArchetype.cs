using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Wolf NPC — hostile beast, pack hunter, moderate difficulty.</summary>
public class WolfArchetype : NpcArchetype
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

    protected override KeywordInContext[] BuildNarrationKeywordsInContext(string name)
        => new[]
        {
            KeywordInContext.Parse("a grey <wolf> lurking nearby"),
            KeywordInContext.Parse("some yellow <eyes> gleaming in the shadow"),
            KeywordInContext.Parse("a low <howl> carried on the wind"),
            KeywordInContext.Parse("some bared <fangs> catching the light"),
        };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a {name.ToLowerInvariant()} watches from the shadows, yellow eyes gleaming";
}
