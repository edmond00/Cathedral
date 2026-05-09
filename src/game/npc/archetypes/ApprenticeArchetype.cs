using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Apprentice or journeyman attached to a village master craftsman.</summary>
public class ApprenticeArchetype : CraftsmanArchetype
{
    public override string ArchetypeId => "apprentice";
    public override int    ModiMentisCount => 6;

    public override string[] NamePool => new[]
    {
        "Young Edmund", "Cuthbert Lad", "Hob Apprentice", "Tibb Smith-boy",
        "Wat Hammers", "Stephen Bellows", "Roger Coalboy", "Dickon Forge",
    };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a soot-faced youth pauses at his work, eyeing you warily — {name}, an apprentice";

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, an apprentice in a village workshop — bound to the master craftsman for years yet. You do the dirty work: fetching coal, sweeping shavings, stoking fires.

You speak deferentially when the master is near and more freely when he isn't. You know the trade gossip — who is behind on payments, which orders are late — but you'd rather not be the one caught telling.

You are tired most days. You are dreaming of being a journeyman.";
}
