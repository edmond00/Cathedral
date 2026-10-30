using Cathedral.Game.Narrative;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Stray Cat NPC — cautious beast, non-hostile, territorial.</summary>
public class StrayCatArchetype : NamedNpcArchetype
{
    // A stray that has survived a while: 1–14 years.
    public override int MinAgeDays => 1 * LifetimeStat.DaysPerYear;
    public override int MaxAgeDays => 14 * LifetimeStat.DaysPerYear;

    public override string ArchetypeId => "stray_cat";

    /// <summary>A stray that may allow a hand along its back — or may not.</summary>
    public override PlayActivity? Play => Stroked;
    private static readonly PlayActivity Stroked = new PettingPlay("crouch, hold out a hand to {0} and stroke it along the back until it purrs", 3, claws: true);
    public override Species Species => SpeciesRegistry.Cat;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 4;

    public override string RoleNoun => "cat";
    protected override bool LabelMentionsLocation => false;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a lean cat regards you with narrowed eyes, tail twitching",
        "a ragged-eared cat crouches on a wall, watching every move",
        "a wary cat slinks along the shadow of a fence, low to the ground",
    };
}
