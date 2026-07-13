using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Farm dairymaid — milks cows, churns butter, presses cheese.</summary>
public class DairymaidArchetype : PeasantArchetype
{
    public override string ArchetypeId => "dairymaid";
    public override ItemTag? SellTag => ItemTag.Foodstuff;
    public override ItemTag? BuyTag  => ItemTag.Craftware;
    public override int    ModiMentisCount => 7;

    public override string[] NamePool => new[]
    {
        "Mariot Dairy", "Cecily Curd", "Hawise Pail", "Avice Churn",
        "Joan Whey", "Petronilla Curd", "Editha Pail", "Lufa Churn",
    };

    public override string RoleNoun => "dairymaid";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a quick figure leans against the churn, sleeves rolled, hands red",
        "a brisk figure carries two brimming pails on a yoke",
        "someone skims cream from a shallow pan, apron spotted with milk",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the dairymaid. You start before dawn — milking the cows, then churning, then pressing. By noon the work is mostly done and you are mostly tired.

You speak warmly, with a country directness. You like having someone to talk to while you work. You know which cow is going dry, which is in season, which butter went to which household this week.

You are not a gossip — quite — but you are very current.";
}
