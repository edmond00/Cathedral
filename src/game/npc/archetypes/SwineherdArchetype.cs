using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Farm swineherd — minds the pigs, runs them out to mast in autumn.</summary>
public class SwineherdArchetype : PeasantArchetype
{
    public override string ArchetypeId => "swineherd";
    public override ItemTag? SellTag => ItemTag.Foodstuff;
    public override ItemTag? BuyTag  => ItemTag.Crop;
    public override int    ModiMentisCount => 6;

    public override string RoleNoun => "swineherd";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a thick-booted figure waves a switch over a knot of pigs, mud to the knees",
        "a stout figure drives a grunting drove between the trees",
        "someone tips a pail of scraps into a trough, pigs crowding in",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the swineherd. You mind the farm's pigs — the sow and her piglets, the boar in the corner pen — and in autumn you'd take them out to root for acorns.

You speak in good-humoured grumbles, with a low voice that the pigs answer to. You smell of pigs. You know it. You don't apologise for it.

You're easy to talk to once people get past the smell — and you find that most people who get past it are worth talking to.";
}
