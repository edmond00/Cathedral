using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>
/// Forest charcoal burner — tends the slow-burning mound that turns logs into coal for the village forge.
/// </summary>
public class CharcoalBurnerArchetype : WildernessNpcArchetype
{
    public override string ArchetypeId => "charcoal_burner";
    public override int    ModiMentisCount => 7;

    public override string[] NamePool => new[]
    {
        "Cuthbert Coalman", "Edmund Char", "Walter Cinder", "Wymark Coalwood",
        "Aldhelm Char", "Hugh Coalman", "Robin Cinder", "Tibb Coalwood",
    };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a soot-blackened figure tends a smouldering mound of earth, smoke curling — {name}, the charcoal burner";

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a charcoal burner. You tend the great smouldering mound day and night for a week at a time, turning logs to charcoal that the village forge will burn.

You speak rarely, in a slow voice, as if measuring smoke. You are alone for long stretches and when you do meet someone you talk a little too much, then catch yourself.

You know the smell of every kind of wood when it chars.";
}
