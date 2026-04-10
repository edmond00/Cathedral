using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Stray Dog NPC — feral beast, hostile, unpredictable.</summary>
public class StrayDogArchetype : NamedNpcArchetype
{
    public override string ArchetypeId => "stray_dog";
    public override Species Species => SpeciesRegistry.Dog;
    public override bool DefaultHostile => true;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 5;

    public override string[] NamePool => new[]
    {
        "Gaunt Dog", "Feral Dog", "Snarling Dog", "Scarred Dog",
        "Mangy Hound", "Lean Cur", "Wild Hound", "Half-Starved Dog"
    };

    protected override KeywordInContext[] BuildNarrationKeywordsInContext(string name)
        => new[]
        {
            KeywordInContext.Parse("a gaunt <dog> circling with bared teeth"),
            KeywordInContext.Parse("a low <snarl> rising from the grass"),
            KeywordInContext.Parse("some matted <fur> raised along its spine"),
            KeywordInContext.Parse("a pair of wild <eyes> locked on you"),
        };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a {name.ToLowerInvariant()} stands hackles raised, a low snarl in its throat";
}
