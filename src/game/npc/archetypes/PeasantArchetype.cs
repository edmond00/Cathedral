using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>
/// Abstract base for field/farm peasant roles (reeve, plowman, reaper, shepherd, etc.).
/// Sets sensible defaults: human, non-hostile, persistent, can speak, civilian.
/// Concrete subclasses set <see cref="ArchetypeId"/>,
/// observation hints, and dialogue prompt text.
/// </summary>
public abstract class PeasantArchetype : NamedNpcArchetype
{
    public override Species Species         => SpeciesRegistry.Human;
    public override bool DefaultPersistent  => true;
    public override int  ModiMentisCount    => 8;
    public override bool CanSpeak           => true;

    /// <summary>Works another man's land. The reeve overrides this — he keeps the accounts.</summary>
    public override SocialCategory? Social  => SocialCategory.Peasant;

    /// <summary>
    /// Any of these can walk you to the reeve: he sets their work, so they have standing to speak to
    /// him and a reason to be listened to. The reeve overrides this back to nothing — he cannot
    /// introduce anybody to himself.
    /// </summary>
    public override IReadOnlyList<string> CanIntroduceToArchetypes => new[] { "reeve" };

    public override string IntroductionRelation => "the reeve";
}
