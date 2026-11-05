using Cathedral.Game.Narrative;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Stray Dog NPC — feral beast, hostile, unpredictable.</summary>
public class StrayDogArchetype : NamedNpcArchetype
{
    // A stray that has survived a while: 1–12 years.
    public override int MinAgeDays => 1 * LifetimeStat.DaysPerYear;
    public override int MaxAgeDays => 12 * LifetimeStat.DaysPerYear;

    public override string ArchetypeId => "stray_dog";

    /// <summary>A stray dog not set against you will take a fuss and a stick to chase.</summary>
    public override PlayActivity? Play => Romped;
    private static readonly PlayActivity Romped = new PettingPlay("scratch {0} behind the ears and throw a stick for it to fetch", 3, claws: true);
    public override Species Species => SpeciesRegistry.Dog;
    public override bool DefaultEnemy => true;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 5;

    public override string RoleNoun => "dog";
    protected override bool LabelMentionsLocation => false;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a gaunt dog stands with hackles raised, a low snarl in its throat",
        "a rangy dog circles at a distance, ribs showing through matted fur",
        "a mangy dog bares its teeth, ears flat against its skull",
    };
}
