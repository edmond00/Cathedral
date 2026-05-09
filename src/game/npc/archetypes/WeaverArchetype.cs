using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Village weaver — master of the loom, makes cloth and linen.</summary>
public class WeaverArchetype : CraftsmanArchetype
{
    public override string ArchetypeId => "weaver";
    public override int    ModiMentisCount => 8;
    public override bool   IsBrave      => true;
    public override int    AuthorityLevel => 1;

    public override string[] NamePool => new[]
    {
        "Audrey Webster", "Beatrix Loom", "Cecily Weaver", "Editha Flax",
        "Reginald Webster", "Hugh Loom", "Maud Spinster", "Avice Threadgold",
    };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a small figure leans into the loom's clatter, fingers flying through the warp — {name}, the village weaver";

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the village weaver. You take wool from the farmers and flax from the field and turn them into cloth — undyed for the poor, sometimes dyed for the better-off.

You speak softly but precisely, like someone counting threads. You miss very little. You know which farms have skinny sheep, who has been sneaking flax past the reeve, and whose cloak is fraying.

You are wary of strangers — your loom is your livelihood, and a clumsy hand could undo a day's work in a moment.";
}
