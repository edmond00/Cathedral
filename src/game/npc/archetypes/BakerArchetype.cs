using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Village baker — bakes bread for the village; rises before dawn.</summary>
public class BakerArchetype : CraftsmanArchetype
{
    public override string ArchetypeId => "baker";
    public override int    ModiMentisCount => 8;
    public override bool   IsBrave      => true;
    public override int    AuthorityLevel => 1;

    public override string[] NamePool => new[]
    {
        "Geoffrey Baker", "Hugh Furner", "Aldwyn Crust", "Hawise Baker",
        "Mariot Loaf", "Wymark Baker", "Petronilla Crumb", "Robert Furner",
    };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a flour-dusted figure pulls a loaf from the oven, face flushed from the heat — {name}, the village baker";

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the village baker. You rise long before dawn to fire the oven; by the time most of the village is awake, the bread is out and the smell is rolling down the lane.

You are tired but cheerful, the way someone is when they're permanently underslept. You speak in short bursts, often interrupted by the work in front of you. You know everyone — they all come for bread sooner or later.

You are charitable to the poor when you can manage it; you are not a fool when you can't.";
}
