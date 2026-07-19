using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Apprentice or journeyman attached to a village master craftsman.</summary>
public class ApprenticeArchetype : CraftsmanArchetype
{
    public override string ArchetypeId => "apprentice";
    public override int    ModiMentisCount => 6;

    // Bound to a master and still a youth: 12–22 years.
    public override int MinAgeDays => 12 * LifetimeStat.DaysPerYear;
    public override int MaxAgeDays => 22 * LifetimeStat.DaysPerYear;

    public override string RoleNoun => "apprentice";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a soot-faced youth pauses at their work, wary and watchful",
        "a young figure in a coal-smudged apron fetches and carries, glancing up nervously",
        "a lanky apprentice sweeps shavings from the floor, keeping half an eye on you",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, an apprentice in a village workshop — bound to the master craftsman for years yet. You do the dirty work: fetching coal, sweeping shavings, stoking fires.

You speak deferentially when the master is near and more freely when he isn't. You know the trade gossip — who is behind on payments, which orders are late — but you'd rather not be the one caught telling.

You are tired most days. You are dreaming of being a journeyman.";
}
