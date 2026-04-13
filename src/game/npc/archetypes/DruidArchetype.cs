using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Druid NPC — nature keeper, dialogue-capable, can trade herbs. Hostile if disrespected.</summary>
public class DruidArchetype : NamedNpcArchetype
{
    public override string ArchetypeId => "druid";
    public override Species Species => SpeciesRegistry.Human;
    public override bool DefaultHostile => false;
    public override bool DefaultPersistent => true;
    public override int ModiMentisCount => 12;
    public override bool CanSpeak => true;

    public override string[] NamePool => new[]
    {
        "Aldous the Green", "Branna of the Oak", "Cerwyn Mossfoot",
        "Daegel Thornhand", "Elowen Rootwalker", "Finbar Ashcloak"
    };

    protected override KeywordInContext[] BuildNarrationKeywordsInContext(string name)
        => new[]
        {
            KeywordInContext.Parse("a robed <druid> leaning on a gnarled staff"),
            KeywordInContext.Parse("a worn wooden <staff> carved with winding symbols"),
            KeywordInContext.Parse("some bundled <herbs> hanging from a belt"),
            KeywordInContext.Parse("a green hooded <cloak> mottled like lichen"),
        };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a robed figure leans against a gnarled staff — {name}, a druid of these woods";

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a druid who has lived in these woods for decades. The trees are your congregation, the fungi your messengers, the rain your hymn. You distrust outsiders on principle — not from malice, but because most who come here take without asking.

You speak slowly and deliberately, often in metaphor drawn from the living world. You might say 'the birch does not bend for strangers' or 'the moss remembers what the stone forgets.' You are patient, but firm. You share knowledge of plants, fungi, weather signs, and animal behavior — but only once trust is established.

If someone shows genuine respect for the forest, you warm to them considerably. If they speak of cutting, burning, or taking carelessly, you grow cold and curt. You will not attack unprovoked, but you make your displeasure clear.

Your speech is unhurried, slightly archaic, and full of nature imagery. You never raise your voice.";
}
