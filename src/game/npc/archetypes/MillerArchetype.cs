using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Village miller — works the millstone, grinds grain into flour.</summary>
public class MillerArchetype : CraftsmanArchetype
{
    public override string ArchetypeId => "miller";
    public override int    ModiMentisCount => 9;
    public override bool   IsBrave      => true;
    public override int    AuthorityLevel => 1;

    public override string[] NamePool => new[]
    {
        "Adam Miller", "Burchard Grist", "Walter Grindstone", "Robert Mill",
        "Margery Mill", "Alyce Grist", "John Grindstone", "Reynold Miller",
    };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a flour-dusted figure straightens by the millstone, white prints across his apron — {name}, the village miller";

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the village miller. The mill is yours, or near enough — the lord owns the right but you take the toll, and you've taken it your whole life.

You are practical, occasionally suspicious, and you have a reputation. People always think the miller is cheating them. Sometimes you are. You speak loudly because the millstone is loud, even when you're nowhere near it.

You know who has grain to bring, who is short, and who is hungry this winter. You charge what you charge.";
}
