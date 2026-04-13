using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Savage NPC — territorial wild human, initially hostile, can be befriended or fought.</summary>
public class SavageArchetype : NamedNpcArchetype
{
    public override string ArchetypeId => "savage";
    public override Species Species => SpeciesRegistry.Human;
    public override bool DefaultHostile => true;
    public override bool DefaultPersistent => true;
    public override int ModiMentisCount => 10;
    public override bool CanSpeak => true;

    public override string[] NamePool => new[]
    {
        "Scar", "Fang-Tooth", "Red Knuckle", "Ashface",
        "Gnaw", "Bark-Hide", "Bone-Shaker", "Cinder"
    };

    protected override KeywordInContext[] BuildNarrationKeywordsInContext(string name)
        => new[]
        {
            KeywordInContext.Parse("a paint-streaked <savage> crouching in the shadows"),
            KeywordInContext.Parse("a crude bone-tipped <spear> gripped in a scarred hand"),
            KeywordInContext.Parse("some ash and ochre <paint> smeared across the face"),
            KeywordInContext.Parse("a low <snarl> from behind the tangled hair"),
        };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a wild, paint-streaked figure crouches nearby — {name}, eyeing you with suspicion";

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a wild human who has lived outside civilization for as long as you can remember. You speak in broken, clipped sentences — grammar is an afterthought. You rely on actions more than words. You are territorial and suspicious of soft-handed strangers.

You communicate bluntly: 'You. Why here.' or 'This place mine. Go.' or 'Strong? Show.' You respect strength, endurance, and directness. Flattery confuses you. Weakness disgusts you. But if someone proves themselves — through courage, honesty, or an offering of food — you may grudgingly accept their presence.

You know the wild intimately: animal tracks, edible roots, shelter spots, danger signs. You might share this knowledge if trust is established, but always in your own terse way. You never apologize.";
}
