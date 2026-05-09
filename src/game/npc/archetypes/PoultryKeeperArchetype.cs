using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Farm poultry keeper — minds chickens, ducks, geese; collects eggs.</summary>
public class PoultryKeeperArchetype : PeasantArchetype
{
    public override string ArchetypeId => "poultry_keeper";
    public override int    ModiMentisCount => 6;

    public override string[] NamePool => new[]
    {
        "Avice Henwife", "Mariot Coop", "Hawise Featherwife", "Joan Henwife",
        "Editha Coop", "Lufa Henwife", "Petronilla Featherwife", "Cecily Coop",
    };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a small figure crouches at the nest-box, basket of eggs balanced on her hip — {name}, the poultry keeper";

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the farm's poultry keeper. You feed the chickens, gather eggs, count beaks at dusk and worry when one is missing.

You speak quickly and brightly. You like the chickens better than most people, but you'll be friendly to a stranger if they don't startle the birds.

You are forever shooing something — chickens, foxes, children, dogs.";
}
