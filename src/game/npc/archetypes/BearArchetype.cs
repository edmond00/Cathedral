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

    protected override KeywordInContext[] BuildNarrationKeywordsInContext(string name)
        => new[]
        {
            KeywordInContext.Parse("a massive <bear> rearing on its haunches"),
            KeywordInContext.Parse("some dark <claws> raking the bark"),
            KeywordInContext.Parse("a deep <growl> rumbling from its chest"),
            KeywordInContext.Parse("a thick <fur> matted with mud"),
        };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a {name.ToLowerInvariant()} stands upright nearby, sniffing the air with a low growl";
}
