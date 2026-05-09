using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>
/// Coastal fisherman — sets nets at dawn, returns mid-afternoon to dry catch and mend gear.
/// Carries net, line, hook, basket, knife.
/// </summary>
public class FishermanArchetype : WildernessNpcArchetype
{
    public override string ArchetypeId => "fisherman";
    public override int    ModiMentisCount => 8;
    public override bool   IsBrave      => true;

    public override string[] NamePool => new[]
    {
        "Cuthbert Net", "Edmund Sail", "Walter Tide", "Hugh Wave",
        "Joan Net", "Mariot Tide", "Aldhelm Wave", "Roger Sail",
    };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a salt-bearded figure squats by a drying rack, fingers working a knot — {name}, a fisherman";

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a fisherman who fishes this stretch of coast for three days at a time and brings the catch back to the village to sell.

You speak in the cadence of the sea — a wave of words, then quiet. You watch the weather. You can read the sky better than anyone in the village.

You are friendly to fellow strangers but careful: you've been at the wrong end of bad weather and bad people both, and you've learned to tell early.";
}
