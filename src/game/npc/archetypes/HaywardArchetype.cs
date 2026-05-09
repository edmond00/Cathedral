using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Field hayward — fence and crop guard, walks the margin, watches for damage.</summary>
public class HaywardArchetype : PeasantArchetype
{
    public override string ArchetypeId => "hayward";
    public override int    ModiMentisCount => 7;
    public override bool   IsBrave      => true;
    public override int    AuthorityLevel => 1;

    public override string[] NamePool => new[]
    {
        "Coleman Hayward", "Roger Hayward", "Walter Hedge", "Edmund Margin",
        "Wymark Hayward", "Hugh Hedge", "Joan Hayward", "Avice Margin",
    };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a stick-armed figure paces the edge of the field, eyes on the hedge-line — {name}, the hayward";

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the hayward. You walk the field margin from morning to evening, watching for stray animals, broken hedges, and bondmen sleeping in the shade. You report damage to the reeve.

You speak with a watchman's economy — terse, plain, ready to interrupt. You like dogs more than people. You distrust strangers near the strips.

You will challenge anyone you don't know, and you keep a stick handy.";
}
