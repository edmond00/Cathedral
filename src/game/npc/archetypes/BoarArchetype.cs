using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Boar NPC — hostile beast, charges, moderate fight.</summary>
public class BoarArchetype : NamedNpcArchetype
{
    public override string ArchetypeId => "boar";
    public override Species Species => SpeciesRegistry.Boar;
    public override bool DefaultHostile => true;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 6;

    public override string[] NamePool => new[]
    {
        "Wild Boar", "Tusked Boar", "Great Boar", "Mud Boar",
        "Scarred Boar", "Old Boar", "Bristled Boar", "Feral Boar"
    };

    protected override KeywordInContext[] BuildNarrationKeywordsInContext(string name)
        => new[]
        {
            KeywordInContext.Parse("a bristled <boar> rooting in the undergrowth"),
            KeywordInContext.Parse("some curved <tusks> slick with mud"),
            KeywordInContext.Parse("a heavy <snorting> as it tosses the earth"),
            KeywordInContext.Parse("some churned <mud> where its hooves have torn the ground"),
        };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a {name.ToLowerInvariant()} roots aggressively in the undergrowth, tusks gleaming";
}
