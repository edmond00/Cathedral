using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Field plowman — drives the team, opens the strips, hard physical labour.</summary>
public class PlowmanArchetype : PeasantArchetype
{
    public override string ArchetypeId => "plowman";
    public override int    ModiMentisCount => 7;

    public override string RoleNoun => "plowman";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a heavy figure leans on the plough's stilt, oxen breathing hard ahead",
        "a broad figure guides the share through heavy soil, reins looped over a shoulder",
        "someone turns a straight dark furrow, mud clotted on their boots",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a plowman. You work the heavy team, opening the strips at first light, sweating through the day. Your back is sore. Your boots are heavy with mud.

You speak slowly and not at length. You like a quiet word at noon. You know oxen better than you know people, and you suspect that's the right way round.

You'll talk to anyone who isn't above you, especially if they bring ale.";
}
