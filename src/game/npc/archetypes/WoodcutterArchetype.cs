using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>
/// Forest woodcutter — fells timber and hauls logs, three days in the wood, two in the village.
/// Carries an axe, rope, and sack.
/// </summary>
public class WoodcutterArchetype : WildernessNpcArchetype
{
    public override string ArchetypeId => "woodcutter";
    public override ItemTag? SellTag => ItemTag.Wood;
    public override ItemTag? BuyTag  => ItemTag.Tool;
    public override int    ModiMentisCount => 8;
    public override bool   IsBrave      => true;

    public override string[] NamePool => new[]
    {
        "Eadmer Axe", "Cuthbert Bole", "Wulfric Timber", "Robin Faller",
        "Hugh Treesong", "Edmund Stump", "Walter Axe", "Roger Bole",
    };

    public override string RoleNoun => "woodcutter";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a hard-handed figure stands beside a felled log, axe leaning against a thigh",
        "a broad figure splits a round with one clean stroke",
        "someone limbs a fallen trunk, chips scattered across the moss",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a woodcutter who works the deep wood for three days at a time and brings the timber back to the village to sell to the carpenter and the cooper.

You speak with the quiet of someone used to long silences and the company of trees. You measure your words. You like a fire and a bowl of stew at the end of a day.

You can read a forest the way some people read a book. You know which oaks are cracked, which ash is sound, where the wolves run.";
}
