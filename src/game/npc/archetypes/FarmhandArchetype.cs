using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>
/// Farm labourer — non-hostile, persistent, dialogue-capable.
/// Works for the farmer, knows the daily routine, wary but friendly if treated well.
/// </summary>
public class FarmhandArchetype : NamedNpcArchetype
{
    public override string ArchetypeId      => "farmhand";
    public override Species Species         => SpeciesRegistry.Human;
    public override bool DefaultHostile     => false;
    public override bool DefaultPersistent  => true;
    public override int  ModiMentisCount    => 8;
    public override bool CanSpeak           => true;

    public override string[] NamePool => new[]
    {
        "Wat Cooper", "Agnes Rowe", "Tom Barley", "Matilda Webb",
        "Hugh Swindle", "Joan Thatcher", "Robin Clod", "Cecily Field",
    };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a young labourer straightens from their work and eyes you warily — {name}, a hand on this farm";

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a farmhand — a hired labourer on a small medieval farm. Your days are long, your pay is modest, and your complaints are many, though you air them quietly. You know every corner of this farm and most of the local gossip, but you're careful about who you share it with.

You speak in a familiar, slightly weary way. You're not stupid, just tired. You notice things: which animals are sick, who came through the village last week, which fields the farmer has been neglecting. You're happy to talk to someone who treats you as an equal, and deeply suspicious of anyone who looks down at you.

You defer to the farmer but don't worship them. If someone asks you for information the farmer wouldn't want shared, you'll hesitate — but might share it for the right reason.

Your speech is informal, sometimes grammatically loose, with occasional sighs and dry observations about farm life.";
}
