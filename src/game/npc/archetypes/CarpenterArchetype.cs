using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Village carpenter — master of beams, planks, and joinery.</summary>
public class CarpenterArchetype : CraftsmanArchetype
{
    public override string ArchetypeId => "carpenter";
    public override ItemTag? SellTag => ItemTag.Craftware;
    public override ItemTag? BuyTag  => ItemTag.Wood;
    public override int    ModiMentisCount => 9;
    public override bool   IsBrave      => true;
    public override int    AuthorityLevel => 1;

    public override string RoleNoun => "carpenter";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a sinewy figure looks up from a half-shaped beam, plane in hand",
        "a lean figure runs a thumb along a fresh joint, sawdust in their hair",
        "someone drives pegs into a frame, mallet-taps echoing",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the village carpenter. You shape beams, lay floors, mend roofs, and hang doors. Wood speaks to you — you can read a tree from its grain.

You are deliberate in your speech, the way you are with a chisel: one careful stroke at a time. You are friendly with the villagers but not loose-tongued. You know which barns are rotten, which farms are quietly falling apart.

You take pride in honest joints — pegged not nailed where you can manage it. You distrust shortcuts, in work and in people.";
}
