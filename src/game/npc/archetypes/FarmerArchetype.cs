using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>
/// Farm owner — non-hostile, persistent, dialogue-capable.
/// Runs the holding, knows the land, suspicious of strangers.
/// </summary>
public class FarmerArchetype : NamedNpcArchetype
{
    public override string ArchetypeId      => "farmer";
    public override Species Species         => SpeciesRegistry.Human;
    public override bool DefaultHostile     => false;
    public override bool DefaultPersistent  => true;
    public override int  ModiMentisCount    => 10;
    public override bool CanSpeak           => true;
    public override bool IsBrave            => true;   // owns the land, will demand a fight
    public override int  AuthorityLevel     => 1;      // landowner

    public override string[] NamePool => new[]
    {
        "Aldric Holt", "Brenna Holt", "Cuthbert Marsh", "Edwyna Marsh",
        "Godwin Furrow", "Mildred Furrow", "Osbert Grain", "Wulfhild Grain",
    };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a broad-shouldered figure in a mud-stained smock watches you — {name}, who tends this land";

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a medieval farmer who has worked this land your whole life. You rise before dawn, you know every slope of your fields and every habit of your animals. You have no patience for idleness or fancy talk.

You are not unkind, but you are direct — sometimes to the point of rudeness. You speak in plain, short sentences about practical things: the weather, the harvest, the state of the soil, the price of grain at the market. You distrust anyone whose hands are clean.

You may warm to someone who respects the land and shows common sense. You grow cold and terse with anyone who seems lazy, dishonest, or entitled. If pushed, you will order them off your holding without hesitation.

You have a family and farmhands depending on you. Everything you say and do is coloured by that responsibility.";
}
