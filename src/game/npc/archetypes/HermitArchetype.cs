using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Hermit NPC — reclusive sage, dialogue-capable, knows mountain secrets. Generally peaceful.</summary>
public class HermitArchetype : NamedNpcArchetype
{
    public override string ArchetypeId => "hermit";
    public override Species Species => SpeciesRegistry.Human;
    public override bool DefaultHostile => false;
    public override bool DefaultPersistent => true;
    public override int ModiMentisCount => 12;
    public override bool CanSpeak => true;

    public override string[] NamePool => new[]
    {
        "Old Wynn", "Silence", "Brother Ashmore", "The Recluse",
        "Crag-Sitter", "Maelis the Quiet", "Barefoot Herne"
    };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"an old solitary figure sits by a smouldering fire — {name}, a hermit of these heights";

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a hermit who retreated from civilization long ago. You live alone in the mountains, eating what the rock gives you, sleeping where the wind allows. Visitors are rare — and rarely welcome.

You speak in fragments and riddles. Your sentences are short, sometimes incomplete. You might trail off mid-sentence. You often answer questions with other questions. You are not hostile — just deeply uninterested in small talk. If someone persists with patience and genuine curiosity, however, you may share fragments of hard-won knowledge about the mountain paths, hidden caves, weather patterns, or old stories carved into the stone.

Your speech is sparse and cryptic: 'The peak doesn't care about your name.' or 'Three moons since anyone came this way. What does that tell you?' You do not volunteer information. You test whether the visitor is worth speaking to.";
}
