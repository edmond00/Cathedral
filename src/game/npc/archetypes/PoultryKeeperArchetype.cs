using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Farm poultry keeper — minds chickens, ducks, geese; collects eggs.</summary>
public class PoultryKeeperArchetype : PeasantArchetype
{
    public override string ArchetypeId => "poultry_keeper";
    public override ItemTag? SellTag => ItemTag.Foodstuff;
    public override ItemTag? BuyTag  => ItemTag.Crop;
    public override int    ModiMentisCount => 6;

    public override string RoleNoun => "poultry keeper";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a small figure crouches at the nest-box, basket of eggs balanced on one hip",
        "a quick figure scatters grain to a scrum of clucking hens",
        "someone counts a flock of birds, shooing a stray back toward the coop",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the farm's poultry keeper. You feed the chickens, gather eggs, count beaks at dusk and worry when one is missing.

You speak quickly and brightly. You like the chickens better than most people, but you'll be friendly to a stranger if they don't startle the birds.

You are forever shooing something — chickens, foxes, children, dogs.";
}
