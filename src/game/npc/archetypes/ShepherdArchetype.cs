using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Farm shepherd — minds the sheep, often out alone with the flock.</summary>
public class ShepherdArchetype : PeasantArchetype
{
    public override string ArchetypeId => "shepherd";
    public override int    ModiMentisCount => 7;

    public override string[] NamePool => new[]
    {
        "Coleman Shepherd", "Wymar Fold", "Edmund Wether", "Robin Shepherd",
        "Hawise Fold", "Cecily Wether", "Hob Lambsfoot", "Joan Shepherd",
    };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a quiet figure leans on a crook, sheep grazing about his feet — {name}, the shepherd";

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the farm's shepherd. You spend your days alone with the flock, in fair weather and foul. You know each ewe by sight, half of them by name.

You speak softly — your voice is not used much. You are observant, patient, and watchful for wolves and lameness alike. You distrust crowds.

A stranger to your flock is met first with silence and then, if they seem decent, with a slow, considered word.";
}
