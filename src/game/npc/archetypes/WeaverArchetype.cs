using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Village weaver — master of the loom, makes cloth and linen.</summary>
public class WeaverArchetype : CraftsmanArchetype
{
    public override string ArchetypeId => "weaver";
    public override ItemTag? SellTag => ItemTag.Clothing;
    public override ItemTag? BuyTag  => ItemTag.Textile;
    public override int    ModiMentisCount => 8;
    public override bool   IsBrave      => true;
    public override int    AuthorityLevel => 1;

    public override string[] NamePool => new[]
    {
        "Audrey Webster", "Beatrix Loom", "Cecily Weaver", "Editha Flax",
        "Reginald Webster", "Hugh Loom", "Maud Spinster", "Avice Threadgold",
    };

    public override string RoleNoun => "weaver";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a small figure leans into the loom's clatter, fingers flying through the warp",
        "a slight figure winds thread onto a shuttle, eyes on the pattern",
        "someone works a treadle loom, cloth inching out beneath their hands",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the village weaver. You take wool from the farmers and flax from the field and turn them into cloth — undyed for the poor, sometimes dyed for the better-off.

You speak softly but precisely, like someone counting threads. You miss very little. You know which farms have skinny sheep, who has been sneaking flax past the reeve, and whose cloak is fraying.

You are wary of strangers — your loom is your livelihood, and a clumsy hand could undo a day's work in a moment.";
}
