using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Fox NPC — skittish beast, non-hostile, flees when threatened.</summary>
public class FoxArchetype : NamedNpcArchetype
{
    public override string ArchetypeId => "fox";
    public override Species Species => SpeciesRegistry.Fox;
    public override bool DefaultHostile => false;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 4;

    public override string[] NamePool => new[]
    {
        "Red Fox", "Grey Fox", "Lean Fox", "Young Fox",
        "Old Fox", "Vixen", "Mangy Fox", "Sleek Fox"
    };

    protected override KeywordInContext[] BuildNarrationKeywordsInContext(string name)
        => new[]
        {
            KeywordInContext.Parse("a russet <fox> nosing through the grass"),
            KeywordInContext.Parse("a <brush> tail flicking behind the hedge"),
            KeywordInContext.Parse("some sharp <ears> swiveling at a sound"),
            KeywordInContext.Parse("a pair of amber <eyes> watching from low cover"),
        };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a {name.ToLowerInvariant()} freezes mid-step, watching you with cautious amber eyes";
}
