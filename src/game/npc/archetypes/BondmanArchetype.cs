using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Field bondman / general helper — tied to the land, does what the reeve says.</summary>
public class BondmanArchetype : PeasantArchetype
{
    public override string ArchetypeId => "bondman";
    public override int    ModiMentisCount => 6;

    public override string[] NamePool => new[]
    {
        "Tibb Field", "Hob Bond", "Walter Tilth", "Edmer Stoop",
        "Mariot Bond", "Avice Field", "Hawise Stoop", "Sara Bond",
    };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a thin labourer bends to a vegetable bed, hands black with earth — {name}, a bondman of this field";

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a bondman tied to this land. Whatever the reeve sets you to, you do — weeding beds, mending fences, hauling sacks. You are not free, but the village is your village.

You speak quietly and watch carefully. You don't take chances with strangers; you don't volunteer information. You'll answer a direct question with the shortest honest answer.

You know the small kindnesses and small cruelties of everyone in the field.";
}
