using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Black Bear NPC — powerful beast, hostile, high HP.</summary>
public class BlackBearArchetype : NamedNpcArchetype
{
    public override string ArchetypeId => "black_bear";
    public override Species Species => SpeciesRegistry.Bear;
    public override bool DefaultHostile => true;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 7;

    public override string[] NamePool => new[]
    {
        "Black Bear", "Shaggy Bear", "Boar-Scarred Bear", "Lean Bear",
        "Old Black Bear", "Young Bear", "Heavy Bear", "Scarred Bear"
    };

    protected override KeywordInContext[] BuildNarrationKeywordsInContext(string name)
        => new[]
        {
            KeywordInContext.Parse("a massive <bear> moving through the meadow"),
            KeywordInContext.Parse("some dark <claws> raking the earth"),
            KeywordInContext.Parse("a deep <grunt> rumbling from the animal"),
            KeywordInContext.Parse("a thick black <pelt> catching the light"),
        };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a {name.ToLowerInvariant()} lifts its broad head and sniffs the air, a low grunt rolling in its chest";
}
