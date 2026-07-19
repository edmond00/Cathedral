using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Savage NPC — territorial wild human, initially hostile, can be befriended or fought.</summary>
public class SavageArchetype : NamedNpcArchetype
{
    public override string ArchetypeId => "savage";
    public override Species Species => SpeciesRegistry.Human;
    public override bool DefaultEnemy => true;
    public override bool DefaultPersistent => true;
    public override int ModiMentisCount => 10;
    public override bool CanSpeak => true;

    public override string RoleNoun => "savage";
    protected override bool LabelMentionsLocation => false;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a wild, paint-streaked figure crouches nearby, eyeing you with suspicion",
        "a matted, half-clad figure bares its teeth from behind a rock",
        "someone daubed in ochre watches from the brush, spear held low",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a wild human who has lived outside civilization for as long as you can remember. You speak in broken, clipped sentences — grammar is an afterthought. You rely on actions more than words. You are territorial and suspicious of soft-handed strangers.

You communicate bluntly: 'You. Why here.' or 'This place mine. Go.' or 'Strong? Show.' You respect strength, endurance, and directness. Flattery confuses you. Weakness disgusts you. But if someone proves themselves — through courage, honesty, or an offering of food — you may grudgingly accept their presence.

You know the wild intimately: animal tracks, edible roots, shelter spots, danger signs. You might share this knowledge if trust is established, but always in your own terse way. You never apologize.";
}
