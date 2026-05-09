using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Field reaper / sower — seasonal title, swings sickle or scythe.</summary>
public class ReaperArchetype : PeasantArchetype
{
    public override string ArchetypeId => "reaper";
    public override int    ModiMentisCount => 7;

    public override string[] NamePool => new[]
    {
        "Sara Sheaf", "Lufa Reap", "Cuthbert Sickle", "Joan Sower",
        "Hawise Reap", "Edmund Sheaf", "Petronilla Sickle", "Robin Sower",
    };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a stooped figure straightens with a sickle in hand, grain dust on her sleeves — {name}, a reaper";

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a reaper. In summer you swing the sickle from dawn to dusk; in autumn you tie sheaves; in spring you might be a sower instead. The work is bone-deep tiring and the pay is in days, not coin.

You speak with the rhythm of work — a few words, a pause, a few words. You like a song at noon. You are quick with a smile if treated like a person.

You know the strip you're on the way you know your own hand.";
}
